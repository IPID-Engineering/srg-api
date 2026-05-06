namespace SRG.Application.Construction;

public record CreateProjectRequest(string Name, string? Description);

public record ProjectResponse(Guid Id, string Name, string? Description, Guid CreatedById, DateTime CreatedAt);

public record CreateSectionRequest(string Name, Guid ProjectId);

public record SectionResponse(Guid Id, string Name, Guid ProjectId, List<InstallationResponse> Installations);

public record CreateInstallationRequest(string Name, string? Description, Guid SectionId);

public record InstallationResponse(Guid Id, string Name, string? Description, Guid SectionId, DateTime CreatedAt);

public record CreateCrewRequest(string Name, Guid ProjectId);

public record AssignCrewRequest(Guid ProjectId);

public record CrewResponse(Guid Id, string Name, Guid ProjectId, Guid CreatedById, DateTime CreatedAt);

public record CreateTeamRequest(string Name, Guid CrewId);

public record TeamResponse(Guid Id, string Name, Guid CrewId);

public record AddPersonRequest(
    string FirstName,
    string LastName,
    Guid CrewId,
    Guid? TeamId);

public record WorkerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Guid CrewId,
    Guid? TeamId,
    Guid CreatedById,
    DateTime CreatedAt);

/// <summary>
/// Email jest opcjonalny, ale wymagany gdy pracownik ma zostać brygadzistą.
/// </summary>
public record CreateSubcontractorWorkerRequest(string FirstName, string LastName, Guid? CrewId, string? Email);

/// <summary>
/// Aktualizacja danych pracownika podwykonawcy.
/// </summary>
public record UpdateSubcontractorWorkerRequest(string? FirstName, string? LastName, string? Email);

public record SubcontractorWorkerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Guid SubcontractorId,
    Guid? CrewId,
    string? Email,
    /// <summary>
    /// Domyślne hasło - widoczne tylko do momentu pierwszego logowania brygadzisty.
    /// Po zmianie hasła pole jest puste.
    /// </summary>
    string? DefaultPassword,
    bool IsForeman,
    DateTime CreatedAt);

public record AssignSubcontractorRequest(Guid SubcontractorId);

public record ProjectSubcontractorResponse(Guid Id, Guid ProjectId, Guid SubcontractorId, string? Email);

public record CreateSubcontractorCrewRequest(string Name);

public record UpdateSubcontractorCrewRequest(string? Name, Guid? CurrentForemanId);

public record SetForemanRequest(Guid ForemanId);

public record SubcontractorCrewResponse(
    Guid Id,
    string Name,
    Guid SubcontractorId,
    Guid? CurrentForemanId,
    string? CurrentForemanName,
    int WorkerCount,
    DateTime CreatedAt);

public record SubcontractorCrewDetailResponse(
    Guid Id,
    string Name,
    Guid SubcontractorId,
    Guid? CurrentForemanId,
    string? CurrentForemanName,
    List<SubcontractorWorkerResponse> Workers,
    List<ForemanHistoryResponse> ForemanHistory,
    DateTime CreatedAt);

public record ForemanHistoryResponse(
    Guid Id,
    Guid ForemanId,
    string ForemanName,
    DateOnly StartDate,
    DateOnly? EndDate);

// Crew Access Management (Admin only) - DEPRECATED, use SubcontractorCrewPmAccess instead
public record CrewAccessResponse(
    Guid Id,
    Guid CrewId,
    string CrewName,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string UserRole,
    DateTime AssignedAt);

public record CrewWithAccessResponse(
    Guid Id,
    string Name,
    string ProjectName,
    string CreatedByName,
    List<CrewAccessUserResponse> AccessList);

public record CrewAccessUserResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime AssignedAt);

public record AssignCrewAccessRequest(Guid UserId);

public record BulkAssignCrewAccessRequest(List<Guid> UserIds);

// Subcontractor Crew PM Access Management (managed by Subcontractor)
public record SubcontractorCrewPmAccessResponse(
    Guid Id,
    Guid CrewId,
    Guid PmUserId,
    string PmEmail,
    string PmFullName,
    DateTime GrantedAt);

public record SubcontractorCrewWithPmAccessResponse(
    Guid Id,
    string Name,
    Guid SubcontractorId,
    Guid? CurrentForemanId,
    string? CurrentForemanName,
    int WorkerCount,
    List<SubcontractorCrewPmAccessResponse> PmAccessList,
    DateTime CreatedAt);

public record GrantPmAccessRequest(Guid PmUserId);
