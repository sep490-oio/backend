using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OIO.Application.Abstractions.Data;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class, IEntity
    {
        return base.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync<TEntity, TId>(
        TId id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder = null,
        CancellationToken cancellationToken = default)
        where TId : IEntityId, new()
        where TEntity : class, IEntity<TId>
    {
        IQueryable<TEntity> query = Set<TEntity>();

        if (queryBuilder is not null)
            query = queryBuilder(query);
        
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public void Insert<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Add(entity);
    }

    public void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities) where TEntity : class, IEntity
    {
        Set<TEntity>().AddRange(entities);
    }
    
    public new void Update<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Update(entity);
    }

    public new void Remove<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        Set<TEntity>().Remove(entity);
    }

    public Task<int> ExecuteSqlAsync(string sql, IEnumerable<SqlParameter> parameters, CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }
}