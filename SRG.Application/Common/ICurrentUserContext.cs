namespace SRG.Application.Common;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Role { get; }
    string? Email { get; }
}
