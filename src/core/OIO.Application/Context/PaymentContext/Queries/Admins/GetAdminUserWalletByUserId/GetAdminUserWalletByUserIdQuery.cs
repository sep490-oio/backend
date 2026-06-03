using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminUserWalletByUserId;

public sealed record GetAdminUserWalletByUserIdQuery(Guid UserId) : IQuery<WalletSummaryDto>;

internal sealed class GetAdminUserWalletByUserIdQueryHandler
    : IQueryHandler<GetAdminUserWalletByUserIdQuery, WalletSummaryDto>
{
    private readonly IDbContext _dbContext;

    public GetAdminUserWalletByUserIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WalletSummaryDto, Error>> Handle(
        GetAdminUserWalletByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = UserId.From(request.UserId);

        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == ownerId && w.WalletFunds.Currency.Id == "VND", cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "User wallet not found.");

        return wallet.ToSummaryDto();
    }
}
