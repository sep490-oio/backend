using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminWithdrawalById;

public sealed record GetAdminWithdrawalByIdQuery(Guid WithdrawalId) : IQuery<AdminWithdrawalRequestDetailDto>;

internal sealed class GetAdminWithdrawalByIdQueryHandler
    : IQueryHandler<GetAdminWithdrawalByIdQuery, AdminWithdrawalRequestDetailDto>
{
    private readonly IDbContext _dbContext;

    public GetAdminWithdrawalByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminWithdrawalRequestDetailDto, Error>> Handle(
        GetAdminWithdrawalByIdQuery request,
        CancellationToken cancellationToken)
    {
        var withdrawalId = WithdrawalRequestId.From(request.WithdrawalId);

        var withdrawal = await _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == withdrawalId, cancellationToken);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal request not found.");

        return PaymentReadModelMapper.ToAdminDetailDto(withdrawal);
    }
}
