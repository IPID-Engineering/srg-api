using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class SubcontractorWorkerService(
    IConstructionRepository construction,
    IDailyReportRepository dailyReports) : ISubcontractorWorkerService
{
    public async Task<SubcontractorWorkerResponse> CreateAsync(
        CreateSubcontractorWorkerRequest request,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException("First name and last name are required.");
        }

        var worker = new SubcontractorWorker
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            SubcontractorId = subcontractorId,
            CrewId = request.CrewId,
            Email = request.Email?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddSubcontractorWorkerAsync(worker, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(worker);
    }

    public async Task<SubcontractorWorkerResponse> UpdateAsync(
        Guid id,
        UpdateSubcontractorWorkerRequest request,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var worker = await construction.GetSubcontractorWorkerByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subcontractor worker was not found.");

        if (worker.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("Subcontractor can update only their own workers.");
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            worker.FirstName = request.FirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            worker.LastName = request.LastName.Trim();
        }

        // Email może być null (usunięcie) lub nową wartością
        if (request.Email != null)
        {
            worker.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        }

        await construction.SaveChangesAsync(cancellationToken);
        return ToResponse(worker);
    }

    public async Task<List<SubcontractorWorkerResponse>> GetMineAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var workers = await construction.GetSubcontractorWorkersAsync(subcontractorId, cancellationToken);
        return workers.Select(ToResponse).ToList();
    }

    public async Task<List<SubcontractorWorkerResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var workers = await construction.GetSubcontractorWorkersByProjectAsync(projectId, cancellationToken);
        return workers.Select(ToResponse).ToList();
    }

    public async Task RemoveAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var worker = await construction.GetSubcontractorWorkerByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subcontractor worker was not found.");

        if (worker.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("Subcontractor can remove only their own workers.");
        }

        // Remove all work hours related to this worker before deleting
        await dailyReports.RemoveWorkHoursBySubcontractorWorkerAsync(id, cancellationToken);

        construction.RemoveSubcontractorWorker(worker);
        await construction.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeleteWorkerImpactResponse> GetDeleteWorkerImpactAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var worker = await construction.GetSubcontractorWorkerByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subcontractor worker was not found.");

        if (worker.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("Subcontractor can view only their own workers.");
        }

        var workHoursCount = await dailyReports.CountWorkHoursBySubcontractorWorkerAsync(id, cancellationToken);

        return new DeleteWorkerImpactResponse(
            worker.Id,
            worker.FirstName,
            worker.LastName,
            workHoursCount
        );
    }

    private static SubcontractorWorkerResponse ToResponse(SubcontractorWorker worker)
    {
        var isForeman = worker.Crew?.CurrentForemanId == worker.Id;
        return new SubcontractorWorkerResponse(
            worker.Id,
            worker.FirstName,
            worker.LastName,
            worker.SubcontractorId,
            worker.CrewId,
            worker.Email,
            // DefaultPassword jest widoczne tylko do momentu pierwszego logowania brygadzisty
            worker.DefaultPassword,
            isForeman,
            worker.InewiEmployeeId,
            worker.CreatedAt);
    }
}
