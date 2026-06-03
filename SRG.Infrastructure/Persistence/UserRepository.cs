using Microsoft.EntityFrameworkCore;
using SRG.Application.Persistence;
using SRG.Domain.Entities;
using SRG.Domain.Enums;

namespace SRG.Infrastructure.Persistence;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByActivationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.ActivationToken == token, cancellationToken);
    }

    public Task<User?> GetByMicrosoftSubjectIdAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.MicrosoftSubjectId == subjectId, cancellationToken);
    }

    public Task<User?> GetByOneTimeLoginTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.OneTimeLoginToken == token, cancellationToken);
    }

    public Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.OrderByDescending(u => u.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.Role == role, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
