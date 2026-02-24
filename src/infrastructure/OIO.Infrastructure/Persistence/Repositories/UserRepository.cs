using Microsoft.EntityFrameworkCore;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.Addresses)
            .Include(u => u.Roles)
            .Include(u => u.Permissions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(UserEmail email, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u =>
                u.Email.Normalized == email.Normalized, ct);
    }

    public async Task<User?> GetByUserNameAsync(UserName userName, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.UserName.Normalized == userName.Normalized, ct);
    }

    public async Task<User?> GetWithTokensAsync(UserId id, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .Include(u => u.Permissions)
            .Include(u => u.RefreshTokenFamilies)
                .ThenInclude(f => f.Tokens)
            .Include(u => u.LoginHistories
                .OrderByDescending(h => h.LoginAt)
                .Take(50))
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <summary>
    /// Overload to get user with tokens by email (used in login flow).
    /// </summary>
    public async Task<User?> GetWithTokensAsync(UserEmail email, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .IgnoreQueryFilters() // Include soft-deleted users to return proper error
            .Include(u => u.Profile)
            .Include(u => u.Roles)
            .Include(u => u.Permissions)
            .Include(u => u.RefreshTokenFamilies.Where(f => f.IsActive))
                .ThenInclude(f => f.Tokens)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u =>
                u.Email.Normalized == email.Normalized, ct);
    }

    public async Task<bool> ExistsByEmailAsync(UserEmail email, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .AnyAsync(u =>
                u.Email.Normalized == email.Normalized, ct);
    }

    public async Task<bool> ExistsByUserNameAsync(UserName userName, CancellationToken ct = default)
    {
        return await _dbContext.Set<User>()
            .AnyAsync(u => u.UserName.Normalized == userName.Normalized, ct);
    }

    public void Add(User user)
    {
        _dbContext.Set<User>().Add(user);
    }

    public void Update(User user)
    {
        _dbContext.Set<User>().Update(user);
    }

    public void Remove(User user)
    {
        _dbContext.Set<User>().Remove(user);
    }
}