using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetMyWithdrawals;

public record WithdrawalFilterParameters : PagedParameters
{
    public string? Status { get; init; }
}

public sealed record GetMyWithdrawalsQuery(
    WithdrawalFilterParameters Parameters) : IQuery<PagedList<WithdrawalRequestDto>>;

internal sealed class GetMyWithdrawalsQueryHandler
    : IQueryHandler<GetMyWithdrawalsQuery, PagedList<WithdrawalRequestDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyWithdrawalsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<WithdrawalRequestDto>, Error>> Handle(
        GetMyWithdrawalsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .Where(x => x.UserId == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = WithdrawalStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Withdrawal.InvalidStatus", "Unsupported withdrawal status.");

            query = query.Where(x => x.Status == status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(count, parameters, cancellationToken);

        return items;
    }
}
