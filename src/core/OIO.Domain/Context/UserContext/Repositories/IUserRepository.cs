using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Domain.Context.UserContext.Repositories;

public interface IUserRepository
{
    #region Queries
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(UserEmail email, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(UserName userName, CancellationToken ct = default);
    #endregion

    
    Task<User?> GetWithTokensAsync(UserId id, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(UserEmail email, CancellationToken ct = default);
    Task<bool> ExistsByUserNameAsync(UserName userName, CancellationToken ct = default);

    #region Commands
    void Add(User user);
    void Update(User user);
    void Remove(User user);
    #endregion
}