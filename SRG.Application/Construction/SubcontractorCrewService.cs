using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using SRG.Application.Auth;
using SRG.Application.Persistence;
using SRG.Domain.Entities;

namespace SRG.Application.Construction;

public class SubcontractorCrewService(
    IConstructionRepository construction,
    IDailyReportRepository dailyReports,
    IWorkOrderRepository workOrders,
    IPasswordService passwordService) : ISubcontractorCrewService
{
    public async Task<SubcontractorCrewResponse> CreateAsync(
        CreateSubcontractorCrewRequest request,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Crew name is required.");
        }

        var crew = new SubcontractorCrew
        {
            Name = request.Name.Trim(),
            SubcontractorId = subcontractorId,
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddSubcontractorCrewAsync(crew, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return ToResponse(crew);
    }

    public async Task<List<SubcontractorCrewResponse>> GetMyCrewsAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetSubcontractorCrewsAsync(subcontractorId, cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    public async Task<SubcontractorCrewDetailResponse> GetByIdAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only view your own crews.");
        }

        return ToDetailResponse(crew);
    }

    public async Task<SubcontractorCrewResponse> UpdateAsync(
        Guid id,
        UpdateSubcontractorCrewRequest request,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only update your own crews.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            crew.Name = request.Name.Trim();
        }

        await construction.SaveChangesAsync(cancellationToken);
        return ToResponse(crew);
    }

    public async Task SetForemanAsync(
        Guid crewId,
        SetForemanRequest request,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only manage your own crews.");
        }

        var worker = await construction.GetSubcontractorWorkerByIdAsync(request.ForemanId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");

        if (worker.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("Worker does not belong to your account.");
        }

        if (worker.CrewId != crewId)
        {
            throw new ValidationException("Worker is not assigned to this crew.");
        }

        // Brygadzista musi mieć ustawiony email - służy jako login
        if (string.IsNullOrWhiteSpace(worker.Email))
        {
            throw new ValidationException("Brygadzista musi mieć ustawiony email. Najpierw dodaj email do pracownika.");
        }

        var currentHistory = await construction.GetCurrentForemanHistoryAsync(crewId, cancellationToken);
        if (currentHistory != null)
        {
            currentHistory.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var newHistory = new SubcontractorForemanHistory
        {
            CrewId = crewId,
            ForemanId = request.ForemanId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
        };

        await construction.AddForemanHistoryAsync(newHistory, cancellationToken);

        crew.CurrentForemanId = request.ForemanId;

        // Generuj hasło tylko jeśli pracownik nie miał wcześniej hasła (nowy brygadzista)
        // lub jeśli poprzednie hasło nie było jeszcze zmienione
        if (string.IsNullOrEmpty(worker.PasswordHash) || worker.MustChangePassword)
        {
            var defaultPassword = GenerateRandomPassword();
            worker.PasswordHash = passwordService.HashPassword(defaultPassword);
            worker.DefaultPassword = defaultPassword; // Widoczne dla subco do momentu pierwszego logowania
            worker.MustChangePassword = true; // Brygadzista musi zmienić hasło przy pierwszym logowaniu
        }

        await construction.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Generuje losowe 8-znakowe hasło składające się z liter i cyfr.
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        return new string(RandomNumberGenerator.GetBytes(8)
            .Select(b => chars[b % chars.Length])
            .ToArray());
    }

    public async Task RemoveAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only remove your own crews.");
        }

        // Clear SubcontractorCrewId from WorkOrders (nullable FK)
        await workOrders.ClearSubcontractorCrewFromWorkOrdersAsync(id, cancellationToken);

        // Delete all related daily reports
        var relatedReports = await dailyReports.GetBySubcontractorCrewAsync(id, cancellationToken);
        if (relatedReports.Count > 0)
        {
            dailyReports.RemoveDailyReports(relatedReports);
        }

        construction.RemoveSubcontractorCrew(crew);
        await construction.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeleteCrewImpactResponse> GetDeleteCrewImpactAsync(Guid id, Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only view your own crews.");
        }

        var reportCount = await dailyReports.CountDailyReportsBySubcontractorCrewAsync(id, cancellationToken);
        var workOrderCount = await workOrders.CountWorkOrdersBySubcontractorCrewAsync(id, cancellationToken);

        return new DeleteCrewImpactResponse(
            crew.Id,
            crew.Name,
            crew.Workers.Count,
            reportCount,
            workOrderCount
        );
    }

    public async Task AssignWorkerToCrewAsync(
        Guid crewId,
        Guid workerId,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only manage your own crews.");
        }

        var worker = await construction.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");

        if (worker.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("Worker does not belong to your account.");
        }

        worker.CrewId = crewId;
        await construction.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveWorkerFromCrewAsync(
        Guid crewId,
        Guid workerId,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only manage your own crews.");
        }

        var worker = await construction.GetSubcontractorWorkerByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");

        if (worker.SubcontractorId != subcontractorId || worker.CrewId != crewId)
        {
            throw new ValidationException("Worker is not in this crew.");
        }

        if (crew.CurrentForemanId == workerId)
        {
            var currentHistory = await construction.GetCurrentForemanHistoryAsync(crewId, cancellationToken);
            if (currentHistory != null)
            {
                currentHistory.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }
            crew.CurrentForemanId = null;
        }

        worker.CrewId = null;
        await construction.SaveChangesAsync(cancellationToken);
    }

    // PM Access Management
    public async Task<SubcontractorCrewWithPmAccessResponse> GetCrewWithPmAccessAsync(
        Guid crewId,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewWithPmAccessAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only view your own crews.");
        }

        return ToResponseWithPmAccess(crew);
    }

    public async Task<SubcontractorCrewPmAccessResponse> GrantPmAccessAsync(
        Guid crewId,
        Guid pmUserId,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only manage access to your own crews.");
        }

        var existingAccess = await construction.GetSubcontractorCrewPmAccessAsync(crewId, pmUserId, cancellationToken);
        if (existingAccess != null)
        {
            throw new ValidationException("PM already has access to this crew.");
        }

        var access = new Domain.Entities.SubcontractorCrewPmAccess
        {
            CrewId = crewId,
            PmUserId = pmUserId,
            GrantedBySubcontractorId = subcontractorId,
            GrantedAt = DateTime.UtcNow,
        };

        await construction.AddSubcontractorCrewPmAccessAsync(access, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return new SubcontractorCrewPmAccessResponse(
            access.Id,
            access.CrewId,
            access.PmUserId,
            access.PmUser?.Email ?? "",
            access.PmUser?.FullName ?? "",
            access.GrantedAt);
    }

    public async Task RevokePmAccessAsync(
        Guid crewId,
        Guid pmUserId,
        Guid subcontractorId,
        CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetSubcontractorCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Crew was not found.");

        if (crew.SubcontractorId != subcontractorId)
        {
            throw new ValidationException("You can only manage access to your own crews.");
        }

        var access = await construction.GetSubcontractorCrewPmAccessAsync(crewId, pmUserId, cancellationToken)
            ?? throw new KeyNotFoundException("PM access was not found.");

        construction.RemoveSubcontractorCrewPmAccess(access);
        await construction.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SubcontractorCrewResponse>> GetCrewsForPmAsync(Guid pmUserId, CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetSubcontractorCrewsByPmAccessAsync(pmUserId, cancellationToken);
        return crews.Select(ToResponse).ToList();
    }

    private static SubcontractorCrewResponse ToResponse(SubcontractorCrew crew)
    {
        return new SubcontractorCrewResponse(
            crew.Id,
            crew.Name,
            crew.SubcontractorId,
            crew.CurrentForemanId,
            crew.CurrentForeman != null ? $"{crew.CurrentForeman.FirstName} {crew.CurrentForeman.LastName}" : null,
            crew.Workers?.Count ?? 0,
            crew.CreatedAt);
    }

    private static SubcontractorCrewDetailResponse ToDetailResponse(SubcontractorCrew crew)
    {
        return new SubcontractorCrewDetailResponse(
            crew.Id,
            crew.Name,
            crew.SubcontractorId,
            crew.CurrentForemanId,
            crew.CurrentForeman != null ? $"{crew.CurrentForeman.FirstName} {crew.CurrentForeman.LastName}" : null,
            crew.Workers?.Select(w => new SubcontractorWorkerResponse(
                w.Id,
                w.FirstName,
                w.LastName,
                w.SubcontractorId,
                w.CrewId,
                w.Email,
                w.DefaultPassword,
                crew.CurrentForemanId == w.Id,
                w.InewiEmployeeId,
                w.CreatedAt)).ToList() ?? [],
            crew.ForemanHistory?.Select(h => new ForemanHistoryResponse(
                h.Id,
                h.ForemanId,
                h.Foreman != null ? $"{h.Foreman.FirstName} {h.Foreman.LastName}" : "Unknown",
                h.StartDate,
                h.EndDate)).OrderByDescending(h => h.StartDate).ToList() ?? [],
            crew.CreatedAt);
    }

    private static SubcontractorCrewWithPmAccessResponse ToResponseWithPmAccess(SubcontractorCrew crew)
    {
        return new SubcontractorCrewWithPmAccessResponse(
            crew.Id,
            crew.Name,
            crew.SubcontractorId,
            crew.CurrentForemanId,
            crew.CurrentForeman != null ? $"{crew.CurrentForeman.FirstName} {crew.CurrentForeman.LastName}" : null,
            crew.Workers?.Count ?? 0,
            crew.PmAccessList?.Select(a => new SubcontractorCrewPmAccessResponse(
                a.Id,
                a.CrewId,
                a.PmUserId,
                a.PmUser?.Email ?? "",
                a.PmUser?.FullName ?? "",
                a.GrantedAt)).ToList() ?? [],
            crew.CreatedAt);
    }
}
