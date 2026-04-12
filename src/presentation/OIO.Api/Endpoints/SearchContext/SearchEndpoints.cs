using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.SearchContext.Commands.BootstrapSync;
using OIO.Application.Context.SearchContext.Queries.GetSuggestions;
using OIO.Application.Context.SearchContext.Queries.GlobalSearch;
using OIO.Application.Context.SearchContext.Queries.SearchAuctions;
using OIO.Application.Context.SearchContext.Queries.SearchItems;
using OIO.Application.Context.SearchContext.Queries.SearchOrders;
using OIO.Application.Context.SearchContext.Queries.SearchShipments;
using OIO.Application.Context.SearchContext.Queries.SearchUsers;
using OIO.Application.Context.SearchContext.Queries.SearchWarehouse;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.SearchContext;

public sealed class SearchEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // ─── Global Search ───────────────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Global, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "category")] string? category = null,
                [FromQuery(Name = "status")] string? status = null) =>
            {
                var filters = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(category)) filters["categoryName.keyword"] = category;
                if (!string.IsNullOrEmpty(status))   filters["status.keyword"] = status;

                var result = await sender.Send(
                    new GlobalSearchQuery(q, page, pageSize, sortBy, desc, filters), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Search.Global)
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<SearchResponseDto<BaseSearchDocument>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Auction-Specific Search ─────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Auctions, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "category")] string? category = null,
                [FromQuery(Name = "status")] string? status = null) =>
            {
                var result = await sender.Send(
                    new SearchAuctionsQuery(q, page, pageSize, sortBy, desc, null, null, status, category), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("SearchAuctions")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<PagedList<AuctionListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Items-Specific Search ───────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Items, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "category")] string? category = null,
                [FromQuery(Name = "status")] string? status = null) =>
            {
                var result = await sender.Send(
                    new SearchItemsQuery(q, page, pageSize, sortBy, desc, status, category), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("SearchItems")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<PagedList<PublicItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Orders Search ───────────────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Orders, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "status")] string? status = null) =>
            {
                var result = await sender.Send(
                    new SearchOrdersQuery(q, page, pageSize, sortBy, desc, status), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName("SearchOrders")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<PagedList<OrderDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Users Search ────────────────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Users, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "role")] string? role = null) =>
            {
                var result = await sender.Send(
                    new SearchUsersQuery(q, page, pageSize, sortBy, desc, role), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadUsers)
            .WithName("SearchUsers")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<PagedList<UserDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Shipments Search ────────────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Shipments, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true,
                [FromQuery(Name = "type")] string? type = null,
                [FromQuery(Name = "provider")] string? provider = null) =>
            {
                var result = await sender.Send(
                    new SearchShipmentsQuery(q, page, pageSize, sortBy, desc, type, provider), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName("SearchShipments")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<SearchResponseDto<ShipmentSearchDocument>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Warehouse Search ────────────────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Warehouse, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10,
                [FromQuery(Name = "sort_by")] string? sortBy = null,
                [FromQuery(Name = "desc")] bool desc = true) =>
            {
                var result = await sender.Send(
                    new SearchWarehouseQuery(q, page, pageSize, sortBy, desc), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName("SearchWarehouse")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<PagedList<WarehouseItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // ─── Auto-complete Suggestions ───────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.Suggestions, async (
                [FromQuery(Name = "q")] string q,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSearchSuggestionsQuery(q), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Search.Suggestions)
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<List<string>>(StatusCodes.Status200OK);

        app.MapGet("api/search/items/suggest", async (
                [FromQuery(Name = "q")] string q,
                IElasticsearchService searchService,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSearchSuggestionsQuery(q, searchService.ItemsIndex), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("GetItemSuggestions")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<List<string>>(StatusCodes.Status200OK);

        app.MapGet("api/search/auctions/suggest", async (
                [FromQuery(Name = "q")] string q,
                IElasticsearchService searchService,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSearchSuggestionsQuery(q, searchService.AuctionsIndex), ct);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("GetAuctionSuggestions")
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<List<string>>(StatusCodes.Status200OK);

        // ─── Seller-Personalized Search ──────────────────────────────────────
        app.MapGet(ApiEndpoint.Url.Search.MySearch, async (
                [FromQuery(Name = "q")] string q,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken ct,
                [FromQuery(Name = "page")] int page = 1,
                [FromQuery(Name = "page_size")] int pageSize = 10) =>
            {
                var filters = new Dictionary<string, string>
                {
                    ["sellerId"] = currentUser.UserId.Value.ToString()
                };

                var result = await sender.Send(
                    new GlobalSearchQuery(q, page, pageSize, null, true, filters), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Search.MySearch)
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces<SearchResponseDto<BaseSearchDocument>>(StatusCodes.Status200OK);

        // ─── Admin: Bootstrap Full Re-sync ───────────────────────────────────
        app.MapPost(ApiEndpoint.Url.Search.Sync, async (
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new BootstrapSearchCommand(), ct);
                return Results.Ok(new { Message = "Full Elasticsearch re-sync completed." });
            })
            .AllowAnonymous() 
            .WithName(ApiEndpoint.Names.Search.SyncSearchIndex)
            .WithTags(ApiEndpoint.Tags.Search)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
