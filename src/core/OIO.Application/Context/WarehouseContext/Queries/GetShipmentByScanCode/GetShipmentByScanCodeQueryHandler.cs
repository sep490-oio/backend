using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetShipmentByScanCode;

internal sealed class GetShipmentByScanCodeQueryHandler
    : IQueryHandler<GetShipmentByScanCodeQuery, List<InboundShipmentDto>>
{
    private readonly IDbContext _dbContext;

    public GetShipmentByScanCodeQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<InboundShipmentDto>, Error>> Handle(
        GetShipmentByScanCodeQuery request,
        CancellationToken          cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ScanCode))
        {
            return Error.Validation("ScanCode", "GetShipment.ScanCodeEmpty", "Scan code cannot be empty.");
        }

        var scanCode = request.ScanCode.Trim();

        // Package-level token: "pkg:{clientOrderCode}" (case-insensitive prefix).
        // Emitted by InboundPackageDetailDto.packageQrToken on the seller page.
        const string PackagePrefix = "pkg:";
        InboundShipment? primary = null;
        if (scanCode.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var clientOrderCode = scanCode.Substring(PackagePrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(clientOrderCode))
            {
                return Error.Validation(
                    "ScanCode",
                    "GetShipment.PackageTokenEmpty",
                    "Package scan token must include a client order code after 'pkg:'.");
            }

            primary = await _dbContext.Set<InboundShipment>()
                .AsNoTracking()
                .Include(s => s.TrackingEvents)
                .FirstOrDefaultAsync(s => s.ClientOrderCode == clientOrderCode, cancellationToken);
        }
        else
        {
            // Raw scan: Guid shipment id first, then ClientOrderCode / CarrierTrackingNumber.
            if (Guid.TryParse(scanCode, out var parsedId))
            {
                var shipmentId = InboundShipmentId.From(parsedId);
                primary = await _dbContext.Set<InboundShipment>()
                    .AsNoTracking()
                    .Include(s => s.TrackingEvents)
                    .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
            }

            primary ??= await _dbContext.Set<InboundShipment>()
                .AsNoTracking()
                .Include(s => s.TrackingEvents)
                .FirstOrDefaultAsync(
                    s => s.ClientOrderCode == scanCode || s.CarrierTrackingNumber == scanCode,
                    cancellationToken);
        }

        if (primary is null)
        {
            return Error.NotFound("InboundShipment.NotFoundByScanCode", $"No shipment found matching scan code '{request.ScanCode}'.");
        }

        // Fetch all shipments in the same batch (sharing ClientOrderCode)
        var batch = await _dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .Where(s => s.ClientOrderCode == primary.ClientOrderCode)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return batch.Select(s => s.ToDto()).ToList();
    }
}
