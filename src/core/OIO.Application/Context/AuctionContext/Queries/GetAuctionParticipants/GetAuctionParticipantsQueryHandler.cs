using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionParticipants;

internal sealed class GetAuctionParticipantsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetAuctionParticipantsQuery, PagedList<AuctionParticipantListItemDto>>
{
    public async Task<Result<PagedList<AuctionParticipantListItemDto>, Error>> Handle(
        GetAuctionParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await dbContext.Set<Auction>().AsNoTracking()
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction == null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId.Value != currentUser.UserId)
            return Error.Forbidden("Auction.NotSeller", "Only the seller can view the participants of this auction.");

        var query = dbContext.Set<AuctionParticipant>().AsNoTracking()
            .Where(x => x.AuctionId == auctionId);

        if (!string.IsNullOrWhiteSpace(p.JoinStatus))
        {
            var jsResult = OIO.Domain.Context.AuctionContext.Enums.ParticipantJoinStatus.FromId(p.JoinStatus);
            if (jsResult.HasValue)
                query = query.Where(x => x.JoinStatus == jsResult.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(p.QualificationStatus))
        {
            var qsResult = OIO.Domain.Context.AuctionContext.Enums.ParticipantQualificationStatus.FromId(p.QualificationStatus);
            if (qsResult.HasValue)
                query = query.Where(x => x.QualificationStatus == qsResult.Value);
        }

        query = query.ApplySort(p, AuctionParticipantMappings.AuctionParticipantListItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var participantEntities = await query
            .ToPagedListAsync(totalCount, p, cancellationToken);

        var userIds = participantEntities.Items
            .Select(x => x.UserId)
            .Distinct()
            .ToList();

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Profile)
            .Where(x => userIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var displayNames = users.ToDictionary(
            x => x.Id.Value,
            x => AuctionNotificationDisplayNames.Resolve(x));

        var items = participantEntities.Items
            .Select(x => x.ToListItemDto(displayNames.TryGetValue(x.UserId.Value, out var dn) ? dn : null))
            .ToList();

        return items.ToPagedList(participantEntities.Metadata);
    }
}
