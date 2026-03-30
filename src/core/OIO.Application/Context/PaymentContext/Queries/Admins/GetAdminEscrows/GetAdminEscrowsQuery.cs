using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Application.Extensions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminEscrows;

public record AdminEscrowFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? BuyerId { get; init; }
    public Guid? SellerId { get; init; }
}

public sealed record GetAdminEscrowsQuery(
    AdminEscrowFilterParameters Parameters) : IQuery<PagedList<EscrowDto>>;

internal sealed class GetAdminEscrowsQueryHandler
    : IQueryHandler<GetAdminEscrowsQuery, PagedList<EscrowDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminEscrowsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<EscrowDto>, Error>> Handle(
        GetAdminEscrowsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(x => x.Order)
            .AsQueryable();

        if (parameters.OrderId.HasValue)
            query = query.Where(x => x.OrderId == OrderId.From(parameters.OrderId.Value));

        if (parameters.BuyerId.HasValue)
            query = query.Where(x => x.Order.BuyerId == UserId.From(parameters.BuyerId.Value));

        if (parameters.SellerId.HasValue)
            query = query.Where(x => x.Order.SellerId == UserId.From(parameters.SellerId.Value));

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = EscrowStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Escrow.InvalidStatus", "Unsupported escrow status.");

            query = query.Where(x => x.Status == status.Value);
        }

        query = query.OrderByDescending(x => x.HeldAt);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(count, parameters, cancellationToken);

        return items;
    }
}
