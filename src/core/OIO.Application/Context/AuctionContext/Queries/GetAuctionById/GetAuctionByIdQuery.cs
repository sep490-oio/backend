using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionById;

public sealed record GetAuctionByIdQuery(Guid AuctionId) : IQuery<AuctionDetailDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAuctionByIdQuery.Check()
            .WithOwnerName("GetAuctionById")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}