using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyWithdrawals;
using OIO.Application.Extensions;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminWithdrawals;

public record AdminWithdrawalFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? UserId { get; init; }
}

public sealed record GetAdminWithdrawalsQuery(
    AdminWithdrawalFilterParameters Parameters) : IQuery<PagedList<WithdrawalRequestDto>>;

internal sealed class GetAdminWithdrawalsQueryHandler
    : IQueryHandler<GetAdminWithdrawalsQuery, PagedList<WithdrawalRequestDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminWithdrawalsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<WithdrawalRequestDto>, Error>> Handle(
        GetAdminWithdrawalsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .AsQueryable();

        if (parameters.UserId.HasValue)
            query = query.Where(x => x.UserId.Value == parameters.UserId.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = WithdrawalStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Withdrawal.InvalidStatus", "Unsupported withdrawal status.");

            query = query.Where(x => x.Status == status.Value);
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
