using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetPendingSellerFees;

public sealed record GetPendingSellerFeesQuery : IQuery<IReadOnlyList<PendingSellerFeeDto>>;

internal sealed class GetPendingSellerFeesQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetPendingSellerFeesQuery, IReadOnlyList<PendingSellerFeeDto>>
{
    public async Task<Result<IReadOnlyList<PendingSellerFeeDto>, Error>> Handle(
        GetPendingSellerFeesQuery request, CancellationToken ct)
    {
        var pendingFees = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Type == TransactionType.Fee
                        && t.Status == TransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PendingSellerFeeDto(
                t.Id.Value,
                t.TransactionNumber.Value,
                t.Amount.Amount,
                t.Currency,
                t.Description,
                t.CreatedAt,
                t.OrderId == null ? (Guid?)null : t.OrderId.Value.Value,
                t.AuctionId == null ? (Guid?)null : t.AuctionId.Value.Value,
                null // Temporarily null, we will populate this next
            ))
            .ToListAsync(ct);

        // Fetch related ItemIds for Inspection Rejection fees
        var inspectionFees = pendingFees.Where(f => f.TransactionNumber.StartsWith("FEE-INSP-REJ-")).ToList();
        if (inspectionFees.Count > 0)
        {
            var inspectionIds = inspectionFees.Select(f =>
            {
                if (Guid.TryParse(f.TransactionNumber.Substring("FEE-INSP-REJ-".Length), out var id)) return id;
                return Guid.Empty;
            }).Where(id => id != Guid.Empty).ToList();

            if (inspectionIds.Count > 0)
            {
                // EF Core cannot translate `inspectionIds.Contains(w.Id.Value)` — member
                // access on a value-converted Id inside Where is not translatable. Compare
                // against the strongly-typed WarehouseItemId list instead so the value
                // converter handles the SQL translation; unwrap `.Value` client-side.
                var inspectionWarehouseItemIds = inspectionIds
                    .Select(id => OIO.Domain.Context.WarehouseContext.ValueObjects.Ids.WarehouseItemId.From(id))
                    .ToList();

                // We use cross-context querying directly on DbContext because it's internal to the query handler
                var inspections = await dbContext.Set<OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.WarehouseItem>()
                    .AsNoTracking()
                    .Where(w => inspectionWarehouseItemIds.Contains(w.Id))
                    .Select(w => new { w.Id, w.ItemId })
                    .ToListAsync(ct);

                var dict = inspections.ToDictionary(x => x.Id.Value, x => x.ItemId);

                for (int i = 0; i < pendingFees.Count; i++)
                {
                    var f = pendingFees[i];
                    if (f.TransactionNumber.StartsWith("FEE-INSP-REJ-"))
                    {
                        if (Guid.TryParse(f.TransactionNumber.Substring("FEE-INSP-REJ-".Length), out var id) && dict.TryGetValue(id, out var itemId))
                        {
                            pendingFees[i] = f with { ItemId = itemId };
                        }
                    }
                }
            }
        }

        return pendingFees;
    }
}
