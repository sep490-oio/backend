using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminTransactionById;

public sealed record GetAdminTransactionByIdQuery(Guid TransactionId) : IQuery<PaymentTransactionDto>;

internal sealed class GetAdminTransactionByIdQueryHandler
    : IQueryHandler<GetAdminTransactionByIdQuery, PaymentTransactionDto>
{
    private readonly IDbContext _dbContext;

    public GetAdminTransactionByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentTransactionDto, Error>> Handle(
        GetAdminTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var transactionId = TransactionId.From(request.TransactionId);

        var transaction = await _dbContext.Set<Transaction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (transaction is null)
            return Error.NotFound("Transaction.NotFound", "Transaction not found.");

        return PaymentReadModelMapper.ToDto(transaction);
    }
}
