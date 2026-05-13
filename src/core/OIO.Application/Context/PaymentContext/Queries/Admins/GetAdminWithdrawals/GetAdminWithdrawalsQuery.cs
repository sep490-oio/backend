using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminWithdrawals;

public record AdminWithdrawalFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? UserId { get; init; }
    public string? SearchTerm { get; init; }
}

public sealed record GetAdminWithdrawalsQuery(
    AdminWithdrawalFilterParameters Parameters) : IQuery<PagedList<AdminWithdrawalRequestDetailDto>>;

internal sealed class GetAdminWithdrawalsQueryHandler
    : IQueryHandler<GetAdminWithdrawalsQuery, PagedList<AdminWithdrawalRequestDetailDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminWithdrawalsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<AdminWithdrawalRequestDetailDto>, Error>> Handle(
        GetAdminWithdrawalsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .AsQueryable();

        if (parameters.UserId.HasValue)
            query = query.Where(x => x.UserId == UserId.From(parameters.UserId.Value));

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = WithdrawalStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Withdrawal.InvalidStatus", "Unsupported withdrawal status.");

            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                (x.BankAccount.AccountHolder != null && x.BankAccount.AccountHolder.ToLower().Contains(term)) ||
                (x.BankAccount.AccountNumber != null && x.BankAccount.AccountNumber.ToLower().Contains(term)) ||
                x.UserId.Value.ToString().ToLower().Contains(term));
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var count = await query.CountAsync(cancellationToken);
        var pagedItems = await query
            .ToPagedListAsync(count, parameters, cancellationToken);

        var userIds = pagedItems.Items.Select(x => x.UserId).Distinct().ToList();
        var adminIds = pagedItems.Items.Where(x => x.ProcessedBy != null).Select(x => x.ProcessedBy!.Value).Distinct().ToList();
        var allUserIds = userIds.Concat(adminIds).Distinct().ToList();

        var users = await _dbContext.Set<Domain.Context.UserContext.Aggregates.Users.User>()
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.SellerProfile)
            .Where(u => allUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
            
        var usersById = users.ToDictionary(u => u.Id.Value);

        var items = pagedItems.Items.Select(x => 
        {
            var dto = x.ToAdminDetailDto();
            var user = usersById.GetValueOrDefault(x.UserId.Value);
            var adminUser = x.ProcessedBy != null ? usersById.GetValueOrDefault(x.ProcessedBy.Value.Value) : null;
            var isKycVerified = user?.SellerProfile?.Status == Domain.Context.UserContext.Enums.SellerProfileStatus.Verified;
            var displayName = user?.Profile?.Name?.DisplayName 
                ?? user?.Profile?.Name?.FullName;
            var adminName = adminUser?.Profile?.Name?.DisplayName 
                ?? adminUser?.Profile?.Name?.FullName ?? adminUser?.UserName.Value;

            return dto with { 
                UserDisplayName = string.IsNullOrWhiteSpace(displayName) ? user?.UserName.Value : displayName,
                UserEmail = user?.Email.Value,
                ProcessedByDisplayName = adminName,
                IsHighRisk = x.Amount > 10_000_000m,
                UserKycVerified = isKycVerified
            };
        }).ToList();

        return new PagedList<AdminWithdrawalRequestDetailDto>(
            items, 
            pagedItems.Metadata.TotalCount, 
            pagedItems.Metadata.CurrentPage, 
            pagedItems.Metadata.PageSize);
    }
}

