using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Application.Persistence;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByActivationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByMicrosoftSubjectIdAsync(string subjectId, CancellationToken cancellationToken = default);
    Task<User?> GetByOneTimeLoginTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
