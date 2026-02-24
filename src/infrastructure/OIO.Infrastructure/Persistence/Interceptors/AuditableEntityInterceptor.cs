using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OIO.Application.Abstractions.Clock;
using OIO.Domain.SeedWork.DomainEvents;
using OIO.Domain.SeedWork.Entities;
using OIO.Infrastructure.Outbox;

namespace OIO.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            UpdateAuditableEntities(eventData.Context, clock.UtcNow);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditableEntities(DbContext context, DateTime utcNow)
    {
        var entries = context.ChangeTracker
            .Entries<IAuditableEntity>()
            .ToList();

        foreach (var entry in entries.Where(entry => entry.State == EntityState.Modified))
        {
            entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = utcNow;
        }
        
        foreach (var entry in entries.Where(entry => entry.State == EntityState.Added))
        {
            entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = utcNow;
        }
    }
}

public sealed class SoftDeleteInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            HandleSoftDelete(eventData.Context, clock.UtcNow);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void HandleSoftDelete(DbContext context, DateTime utcNow)
    {
        var entries = context.ChangeTracker
            .Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            // Prevent hard delete, convert to soft delete
            entry.State = EntityState.Modified;
            entry.Entity.SoftDelete(utcNow);
        }
    }
}

public sealed class ConcurrencyInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            IncrementVersions(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void IncrementVersions(DbContext context)
    {
        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e is { State: EntityState.Modified, Entity: IVersionEntity })
            .ToList();

        foreach (var prop in entries.Select(entry => entry.Property(nameof(IVersionEntity.Version))))
        {
            prop.CurrentValue = (int)prop.CurrentValue! + 1;
        }
    }
}

public class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        InsertOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        InsertOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void InsertOutboxMessages(DbContext? eventDataContext)
    {
        if (eventDataContext is null) return;

        var aggregateRoots = eventDataContext
            .ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();
        
        var domainEvents = aggregateRoots
                .SelectMany(ar => ar.DomainEvents)
                .ToList();
            
        var outboxMessages = domainEvents
            .Select(domainEvent => new OutboxMessage()
            {
                Id = domainEvent.EventId,
                Type = domainEvent.GetType().FullName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredAt = domainEvent.OccurredAt,
                ProcessedAt = null,
                Error = null,
                AttemptCount = 0
            })
            .ToList();

        eventDataContext.Set<OutboxMessage>().AddRange(outboxMessages);
        
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }
    }
}