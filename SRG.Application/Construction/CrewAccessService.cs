using System.ComponentModel.DataAnnotations;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Construction;

public interface ICrewAccessService
{
    Task<List<CrewWithAccessResponse>> GetAllCrewsWithAccessAsync(CancellationToken cancellationToken = default);
    Task<CrewWithAccessResponse> GetCrewWithAccessAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task<CrewAccessResponse> AssignAccessAsync(Guid crewId, Guid userId, Guid assignedById, CancellationToken cancellationToken = default);
    Task RemoveAccessAsync(Guid crewId, Guid userId, CancellationToken cancellationToken = default);
    Task BulkAssignAccessAsync(Guid crewId, List<Guid> userIds, Guid assignedById, CancellationToken cancellationToken = default);
}

public class CrewAccessService(
    IConstructionRepository construction,
    IUserRepository users) : ICrewAccessService
{
    public async Task<List<CrewWithAccessResponse>> GetAllCrewsWithAccessAsync(CancellationToken cancellationToken = default)
    {
        var crews = await construction.GetCrewsWithAccessAsync(cancellationToken);
        return crews.Select(ToCrewWithAccessResponse).ToList();
    }

    public async Task<CrewWithAccessResponse> GetCrewWithAccessAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetCrewWithAccessAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Brygada nie została znaleziona.");

        return ToCrewWithAccessResponse(crew);
    }

    public async Task<CrewAccessResponse> AssignAccessAsync(Guid crewId, Guid userId, Guid assignedById, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetCrewByIdAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Brygada nie została znaleziona.");

        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Użytkownik nie został znaleziony.");

        if (user.Role != UserRole.PM && user.Role != UserRole.Subcontractor)
        {
            throw new ValidationException("Dostęp do brygad można przypisać tylko użytkownikom z rolą PM lub Subcontractor.");
        }

        var existingAccess = await construction.GetCrewAccessAsync(crewId, userId, cancellationToken);
        if (existingAccess != null)
        {
            throw new ValidationException("Ten użytkownik ma już dostęp do tej brygady.");
        }

        var access = new CrewAccess
        {
            CrewId = crewId,
            UserId = userId,
            AssignedById = assignedById,
            AssignedAt = DateTime.UtcNow,
        };

        await construction.AddCrewAccessAsync(access, cancellationToken);
        await construction.SaveChangesAsync(cancellationToken);

        return new CrewAccessResponse(
            access.Id,
            crewId,
            crew.Name,
            userId,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            access.AssignedAt);
    }

    public async Task RemoveAccessAsync(Guid crewId, Guid userId, CancellationToken cancellationToken = default)
    {
        var access = await construction.GetCrewAccessAsync(crewId, userId, cancellationToken)
            ?? throw new KeyNotFoundException("Przypisanie dostępu nie zostało znalezione.");

        construction.RemoveCrewAccess(access);
        await construction.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkAssignAccessAsync(Guid crewId, List<Guid> userIds, Guid assignedById, CancellationToken cancellationToken = default)
    {
        var crew = await construction.GetCrewWithAccessAsync(crewId, cancellationToken)
            ?? throw new KeyNotFoundException("Brygada nie została znaleziona.");

        var existingUserIds = crew.AccessList.Select(a => a.UserId).ToHashSet();

        foreach (var userId in userIds)
        {
            if (existingUserIds.Contains(userId))
            {
                continue;
            }

            var user = await users.GetByIdAsync(userId, cancellationToken);
            if (user == null || (user.Role != UserRole.PM && user.Role != UserRole.Subcontractor))
            {
                continue;
            }

            var access = new CrewAccess
            {
                CrewId = crewId,
                UserId = userId,
                AssignedById = assignedById,
                AssignedAt = DateTime.UtcNow,
            };

            await construction.AddCrewAccessAsync(access, cancellationToken);
        }

        await construction.SaveChangesAsync(cancellationToken);
    }

    private static CrewWithAccessResponse ToCrewWithAccessResponse(Crew crew)
    {
        return new CrewWithAccessResponse(
            crew.Id,
            crew.Name,
            crew.Project?.Name ?? "Unknown",
            crew.CreatedBy?.FullName ?? "Unknown",
            crew.AccessList.Select(a => new CrewAccessUserResponse(
                a.Id,
                a.UserId,
                a.User?.Email ?? "",
                a.User?.FullName ?? "",
                a.User?.Role.ToString() ?? "",
                a.AssignedAt)).ToList());
    }
}
