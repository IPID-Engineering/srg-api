using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public interface IForemanWorkerService
{
    Task<List<ForemanWorkerResponse>> GetMyWorkersAsync(Guid foremanWorkerId, CancellationToken cancellationToken = default);
    Task<ForemanWorkerResponse> AddWorkerAsync(AddForemanWorkerRequest request, Guid foremanWorkerId, CancellationToken cancellationToken = default);
    Task<ForemanWorkerStatsResponse> GetWorkerStatsAsync(Guid workerId, Guid foremanWorkerId, CancellationToken cancellationToken = default);
}

public record AddForemanWorkerRequest(string FirstName, string LastName);

public record ForemanWorkerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Guid? CrewId);

public record ForemanWorkerStatsResponse(
    Guid WorkerId,
    string WorkerName,
    decimal TotalHours,
    int ReportCount,
    decimal AverageHoursPerReport,
    List<WorkerReportSummary> RecentReports);

public record WorkerReportSummary(
    Guid ReportId,
    DateOnly Date,
    decimal Hours,
    string? Notes);

public class ForemanWorkerService(
    IConstructionRepository construction,
    IDailyReportRepository dailyReportRepository) : IForemanWorkerService
{
    public async Task<List<ForemanWorkerResponse>> GetMyWorkersAsync(Guid foremanWorkerId, CancellationToken cancellationToken = default)
    {
        var foreman = await construction.GetSubcontractorWorkerByIdAsync(foremanWorkerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygadzisty.");

        if (foreman.CrewId is null)
        {
            throw new KeyNotFoundException("Brygadzista nie ma przypisanej brygady.");
        }

        var crew = await construction.GetSubcontractorCrewByIdAsync(foreman.CrewId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygady.");

        if (crew.CurrentForemanId != foremanWorkerId)
        {
            throw new ValidationException("Nie jesteś aktualnym brygadzistą tej brygady.");
        }

        var workers = await construction.GetSubcontractorWorkersByCrewAsync(foreman.CrewId.Value, cancellationToken);
        return workers.Select(ToResponse).ToList();
    }

    public async Task<ForemanWorkerResponse> AddWorkerAsync(AddForemanWorkerRequest request, Guid foremanWorkerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException("Imię i nazwisko są wymagane.");
        }

        var foreman = await construction.GetSubcontractorWorkerByIdAsync(foremanWorkerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygadzisty.");

        if (foreman.CrewId is null)
        {
            throw new KeyNotFoundException("Brygadzista nie ma przypisanej brygady.");
        }

        var crew = await construction.GetSubcontractorCrewByIdAsync(foreman.CrewId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygady.");

        if (crew.CurrentForemanId != foremanWorkerId)
        {
            throw new ValidationException("Nie jesteś aktualnym brygadzistą tej brygady.");
        }

        var worker = new SubcontractorWorker
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            SubcontractorId = crew.SubcontractorId,
            CrewId = crew.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddSubcontractorWorkerAsync(worker, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(worker);
    }

    public async Task<ForemanWorkerStatsResponse> GetWorkerStatsAsync(Guid workerId, Guid foremanWorkerId, CancellationToken cancellationToken = default)
    {
        var foreman = await construction.GetSubcontractorWorkerByIdAsync(foremanWorkerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygadzisty.");

        if (foreman.CrewId is null)
        {
            throw new KeyNotFoundException("Brygadzista nie ma przypisanej brygady.");
        }

        var crew = await construction.GetSubcontractorCrewByIdAsync(foreman.CrewId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono brygady.");

        if (crew.CurrentForemanId != foremanWorkerId)
        {
            throw new ValidationException("Nie jesteś aktualnym brygadzistą tej brygady.");
        }

        var worker = await construction.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nie znaleziono pracownika.");

        if (worker.CrewId != crew.Id)
        {
            throw new ValidationException("Ten pracownik nie należy do Twojej brygady.");
        }

        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3));
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow);

        var reports = await dailyReportRepository.GetByCrewForStatsAsync(crew.Id, dateFrom, dateTo, cancellationToken);

        var workerHours = reports
            .SelectMany(r => r.WorkHours.Where(wh => wh.SubcontractorWorkerId == workerId))
            .ToList();

        var totalHours = workerHours.Sum(wh => wh.Hours);
        var reportIds = workerHours.Select(wh => wh.DailyReportId).Distinct().ToHashSet();
        var reportCount = reportIds.Count;
        var avgHours = reportCount > 0 ? totalHours / reportCount : 0;

        var recentReports = reports
            .Where(r => reportIds.Contains(r.Id))
            .OrderByDescending(r => r.Date)
            .Take(10)
            .Select(r => new WorkerReportSummary(
                r.Id,
                r.Date,
                r.WorkHours.Where(wh => wh.SubcontractorWorkerId == workerId).Sum(wh => wh.Hours),
                r.Notes))
            .ToList();

        return new ForemanWorkerStatsResponse(
            workerId,
            $"{worker.FirstName} {worker.LastName}",
            totalHours,
            reportCount,
            avgHours,
            recentReports);
    }

    private static ForemanWorkerResponse ToResponse(SubcontractorWorker worker)
    {
        return new ForemanWorkerResponse(
            worker.Id,
            worker.FirstName,
            worker.LastName,
            worker.CrewId);
    }
}
