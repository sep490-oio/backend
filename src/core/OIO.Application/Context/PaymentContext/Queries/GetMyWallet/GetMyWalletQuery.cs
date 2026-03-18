using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetMyWallet;

public sealed record GetMyWalletQuery() : IQuery<WalletSummaryDto>;

internal sealed class GetMyWalletQueryHandler
    : IQueryHandler<GetMyWalletQuery, WalletSummaryDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyWalletQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<WalletSummaryDto, Error>> Handle(
        GetMyWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == _currentUser.UserId, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Wallet not found.");

        return PaymentReadModelMapper.ToSummaryDto(wallet);
    }
}
