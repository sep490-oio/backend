using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Application.Abstractions.Data;

public interface IDbContext
{
    DbSet<TEntity> Set<TEntity>()
        where TEntity : class, IEntity;
    
    Task<TEntity?> GetByIdAsync<TEntity, TId>(
        TId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder = null,
        CancellationToken cancellationToken = default)
        where TId : IEntityId, new()
        where TEntity : class, IEntity<TId>;
    
    void Insert<TEntity>(TEntity entity)
        where TEntity : class, IEntity;
    
    void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class, IEntity;

    void Update<TEntity>(TEntity entity)
        where TEntity : class, IEntity;
    
    void Remove<TEntity>(TEntity entity)
        where TEntity : class, IEntity;

    Task<int> ExecuteSqlAsync(
        string sql,
        IEnumerable<SqlParameter> parameters,
        CancellationToken cancellationToken = default);
}