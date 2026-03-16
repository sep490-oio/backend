using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Application.Extensions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminTransactions;

public record AdminTransactionFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public string? Type { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrderId { get; init; }
}

public sealed record GetAdminTransactionsQuery(
    AdminTransactionFilterParameters Parameters) : IQuery<PagedList<PaymentTransactionDto>>;

internal sealed class GetAdminTransactionsQueryHandler
    : IQueryHandler<GetAdminTransactionsQuery, PagedList<PaymentTransactionDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminTransactionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<PaymentTransactionDto>, Error>> Handle(
        GetAdminTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Transaction>()
            .AsNoTracking()
            .AsQueryable();

        if (parameters.UserId.HasValue)
            query = query.Where(x => x.UserId == UserId.From(parameters.UserId.Value));

        if (parameters.OrderId.HasValue)
            query = query.Where(x => x.OrderId == OrderId.From(parameters.OrderId.Value));

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = TransactionStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Transaction.InvalidStatus", "Unsupported transaction status.");

            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Type))
        {
            var type = TransactionType.FromId(parameters.Type);
            if (type.HasNoValue)
                return Error.Validation("type", "Transaction.InvalidType", "Unsupported transaction type.");

            query = query.Where(x => x.Type == type.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var count = await query.CountAsync(cancellationToken);
        var items = await query.Page(parameters).ToListAsync(cancellationToken);

        return items
            .Select(PaymentReadModelMapper.ToDto)
            .ToList()
            .ToPagedList(count, parameters);
    }
}
