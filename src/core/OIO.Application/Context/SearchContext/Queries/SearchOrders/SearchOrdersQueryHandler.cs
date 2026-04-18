using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchOrders;

public class SearchOrdersQueryHandler(IElasticsearchService searchService)
    : IRequestHandler<SearchOrdersQuery, PagedList<OrderDto>>
{
    public async Task<PagedList<OrderDto>> Handle(SearchOrdersQuery request, CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Status)) filters["status.keyword"] = request.Status;

        var searchResult = await searchService.SearchAsync<OrderSearchDocument>(
            request.Query,
            [searchService.OrdersIndex],
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            filters,
            cancellationToken: cancellationToken);

        var mappedResults = searchResult.Results.Select(doc => new OrderDto(
            Id: Guid.Parse(doc.Id),
            OrderNumber: doc.OrderNumber,
            AuctionId: !string.IsNullOrEmpty(doc.AuctionId) ? Guid.Parse(doc.AuctionId) : Guid.Empty,
            BuyerId: Guid.Parse(doc.BuyerId),
            SellerId: !string.IsNullOrEmpty(doc.SellerId) ? Guid.Parse(doc.SellerId) : Guid.Empty,
            Status: doc.Status,
            TotalAmount: doc.TotalAmount,
            Currency: doc.Currency,
            CreatedAt: doc.CreatedAt,
            PaymentDueAt: null,
            PaidAt: null,
            ShippedAt: null,
            DeliveredAt: null,
            DecisionWindowEndsAt: null,
            CompletedAt: null,
            CancelledAt: null,
            EscrowStatus: null,
            TrackingNumber: null,
            Return: null,
            BuyerDisplayName: doc.BuyerName,
            SellerDisplayName: null,
            Shipping: null,
            Item: null,
            SellerFulfillment: null,
            BuyerCanUpdateShipping: false,
            AmountPaid: null,
            DepositAppliedAmount: null,
            WalletAppliedAmount: null,
            GatewayPaidAmount: null,
            DirectShipment: null,
            WarehouseOutboundShipment: null
        )).ToList();

        return new PagedList<OrderDto>(
            mappedResults,
            (int)searchResult.Total,
            searchResult.Page,
            searchResult.PageSize);
    }
}
