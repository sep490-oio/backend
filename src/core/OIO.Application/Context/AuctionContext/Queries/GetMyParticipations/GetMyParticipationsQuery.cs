using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyParticipations;

public sealed record GetMyParticipationsQuery(
    GetMyParticipationsFilterParameters Parameters) : IQuery<PagedList<MyParticipationDto>>;
