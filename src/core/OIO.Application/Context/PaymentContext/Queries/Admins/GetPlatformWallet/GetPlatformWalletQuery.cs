using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformWallet;

public sealed record GetPlatformWalletQuery() : IQuery<WalletSummaryDto>;

internal sealed class GetPlatformWalletQueryHandler
    : IQueryHandler<GetPlatformWalletQuery, WalletSummaryDto>
{
    private readonly IDbContext _dbContext;

    public GetPlatformWalletQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WalletSummaryDto, Error>> Handle(
        GetPlatformWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .Include(w => w.WalletTransactions)
            .FirstOrDefaultAsync(w => w.Type == WalletType.Platform, cancellationToken);

        if (wallet is null)
            return Error.NotFound("PlatformWallet.NotFound", "Platform wallet not found.");

        return PaymentReadModelMapper.ToSummaryDto(wallet);
    }
}
