using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetMyWalletTransactionById;

public sealed record GetMyWalletTransactionByIdQuery(Guid TransactionId) : IQuery<WalletTransactionDto>;

internal sealed class GetMyWalletTransactionByIdQueryHandler
    : IQueryHandler<GetMyWalletTransactionByIdQuery, WalletTransactionDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyWalletTransactionByIdQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<WalletTransactionDto, Error>> Handle(
        GetMyWalletTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var transactionId = WalletTransactionId.From(request.TransactionId);

        var transaction = await _dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Include(x => x.Wallet)
            .Include(x => x.Transaction)
            .FirstOrDefaultAsync(
                x => x.Id == transactionId && x.Wallet.UserId == _currentUser.UserId,
                cancellationToken);

        if (transaction is null)
            return Error.NotFound("WalletTransaction.NotFound", "Wallet transaction not found.");

        return PaymentReadModelMapper.ToDto(transaction, transaction.Wallet.WalletFunds.Currency.Id);
    }
}
