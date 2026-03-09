using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAutoBid;

public sealed record GetMyAutoBidQuery(Guid AuctionId) : IQuery<AutoBidDto?>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyAutoBidQuery.Check()
            .WithOwnerName("GetMyAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}