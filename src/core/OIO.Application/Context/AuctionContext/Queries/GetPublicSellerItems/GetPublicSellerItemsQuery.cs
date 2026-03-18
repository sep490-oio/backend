using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicSellerItems;

public record GetPublicSellerItemsFilterParameters : PagedParameters;

public sealed record GetPublicSellerItemsQuery(
    Guid SellerId,
    GetPublicSellerItemsFilterParameters Parameters) : IQuery<PagedList<PublicSellerItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetPublicSellerItemsQuery.Check()
            .WithOwnerName("GetPublicSellerItems")
            .Field(SellerId).NotEmptyGuid();
    }
}
