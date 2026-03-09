using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemById;

public sealed record GetItemByIdQuery(Guid ItemId) : IQuery<ItemDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetItemByIdQuery.Check()
            .WithOwnerName("GetItemById")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}