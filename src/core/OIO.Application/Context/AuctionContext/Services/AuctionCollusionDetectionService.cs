using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.Services;

public interface IAuctionCollusionDetectionService
{
    Task DetectAfterBidPlacedAsync(BidPlacedEvent notification, CancellationToken cancellationToken);
    Task DetectAfterBuyNowReservedAsync(AuctionBuyNowReservedEvent notification, CancellationToken cancellationToken);
    Task ScanAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
}

public sealed record AuctionCollusionFinding(
    string AlertType,
    string SignalType,
    AlertSeverity Severity,
    int Score,
    Guid SellerId,
    IReadOnlyList<Guid> ImplicatedUserIds,
    IReadOnlyList<Guid> BidIds,
    Guid? ReservationId,
    string Window,
    string EvidenceSummary,
    IReadOnlyList<string> PairKeys,
    IReadOnlyList<Guid>? SharedDeviceIds = null,
    IReadOnlyList<string>? SharedIpAddresses = null,
    IReadOnlyList<string>? SharedBidIpAddresses = null,
    decimal? DominancePercent = null,
    IReadOnlyList<Guid>? DominantBidderIds = null,
    IReadOnlyList<Guid>? PriorAuctionIds = null);

internal sealed class AuctionCollusionDetectionService(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IRuntimeSettings runtimeSettings,
    IClock clock,
    ILogger<AuctionCollusionDetectionService> logger)
    : IAuctionCollusionDetectionService
{
    private const string SystemActorRole = "system";
    private const string RiskFlagType = "auction_collusion_suspected";
    private const string AuditAction = "auction_collusion_signal_detected";

    private const string SellerBidderSameDeviceAlertType = "auction_collusion_seller_bidder_same_device";
    private const string SellerBidderSameIpAlertType = "auction_collusion_seller_bidder_same_ip";
    private const string TopBiddersSameDeviceAlertType = "auction_collusion_top_bidders_same_device";
    private const string TopBiddersSameIpAlertType = "auction_collusion_top_bidders_same_ip";
    private const string PingPongAlertType = "auction_collusion_ping_pong_bidding";
    private const string RepeatedPairAlertType = "auction_collusion_repeated_pair";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> StrongAlertTypes =
    [
        SellerBidderSameDeviceAlertType,
        SellerBidderSameIpAlertType,
        TopBiddersSameDeviceAlertType
    ];

    public async Task DetectAfterBidPlacedAsync(
        BidPlacedEvent notification,
        CancellationToken cancellationToken)
    {
        await DetectForBidAsync(
            auctionId: Guid.Parse(notification.AuctionId),
            bidderId: Guid.Parse(notification.BidderId),
            bidId: Guid.Parse(notification.BidId),
            occurredAt: notification.OccurredAt,
            cancellationToken);
    }

    public async Task DetectAfterBuyNowReservedAsync(
        AuctionBuyNowReservedEvent notification,
        CancellationToken cancellationToken)
    {
        await DetectForReservationAsync(
            auctionId: Guid.Parse(notification.AuctionId),
            buyerId: Guid.Parse(notification.BuyerId),
            reservationId: Guid.Parse(notification.ReservationId),
            occurredAt: notification.OccurredAt,
            cancellationToken);
    }

    public async Task ScanAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var auction = await LoadAuctionAsync(auctionId, cancellationToken);
        if (auction is null || !ShouldScanAuction(auction, nowUtc))
            return;

        var latestBid = auction.Bids
            .Where(x => x.Status != BidStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        var latestReservation = auction.BuyNowReservations
            .Where(x => x.IsPendingPayment)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latestBid is not null)
        {
            await DetectForBidAsync(
                auction.Id.Value,
                latestBid.BidderId.Value,
                latestBid.Id.Value,
                nowUtc,
                cancellationToken);
        }

        if (latestReservation is not null && latestReservation.IsActive(nowUtc))
        {
            await DetectForReservationAsync(
                auction.Id.Value,
                latestReservation.BuyerId.Value,
                latestReservation.Id.Value,
                nowUtc,
                cancellationToken);
        }
    }

    private async Task DetectForBidAsync(
        Guid auctionId,
        Guid bidderId,
        Guid bidId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var auction = await LoadAuctionAsync(auctionId, cancellationToken);
        if (auction is null)
            return;

        var config = LoadSettings();
        var findings = await AnalyzeBidSignalsAsync(
            auction,
            bidderId,
            bidId,
            occurredAt,
            config,
            cancellationToken);

        await PersistFindingsAsync(auctionId, findings, occurredAt, cancellationToken);
    }

    private async Task DetectForReservationAsync(
        Guid auctionId,
        Guid buyerId,
        Guid reservationId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var auction = await LoadAuctionAsync(auctionId, cancellationToken);
        if (auction is null)
            return;

        var config = LoadSettings();
        var findings = await AnalyzeReservationSignalsAsync(
            auction,
            buyerId,
            reservationId,
            occurredAt,
            config,
            cancellationToken);

        await PersistFindingsAsync(auctionId, findings, occurredAt, cancellationToken);
    }

    private async Task<List<AuctionCollusionFinding>> AnalyzeBidSignalsAsync(
        Auction auction,
        Guid bidderId,
        Guid bidId,
        DateTime occurredAt,
        AuctionCollusionSettings config,
        CancellationToken cancellationToken)
    {
        var findings = new List<AuctionCollusionFinding>();
        var sellerId = auction.Item.SellerId.Value;
        var topBidders = GetTopDistinctBidderIds(auction, 2);
        var relevantUsers = new HashSet<Guid>(topBidders) { sellerId, bidderId };
        var sessions = await LoadSessionEvidenceAsync(
            relevantUsers,
            config.MaxSessionWindowStart(occurredAt),
            cancellationToken);

        AddSellerBidderFindings(
            findings,
            sessions,
            sellerId,
            bidderId,
            occurredAt,
            bidId,
            config);

        AddTopBidderFindings(
            findings,
            auction,
            sessions,
            sellerId,
            occurredAt,
            bidId,
            config);

        AddPingPongFinding(
            findings,
            auction,
            sellerId,
            occurredAt,
            config);

        var repeatedPairFinding = await CreateRepeatedPairFindingAsync(
            auction.Id.Value,
            sellerId,
            findings,
            occurredAt,
            config,
            cancellationToken);

        if (repeatedPairFinding is not null)
            findings.Add(repeatedPairFinding);

        return findings;
    }

    private async Task<List<AuctionCollusionFinding>> AnalyzeReservationSignalsAsync(
        Auction auction,
        Guid buyerId,
        Guid reservationId,
        DateTime occurredAt,
        AuctionCollusionSettings config,
        CancellationToken cancellationToken)
    {
        var findings = new List<AuctionCollusionFinding>();
        var sellerId = auction.Item.SellerId.Value;
        var sessions = await LoadSessionEvidenceAsync(
            [sellerId, buyerId],
            config.MaxSessionWindowStart(occurredAt),
            cancellationToken);

        AddSellerBidderFindings(
            findings,
            sessions,
            sellerId,
            buyerId,
            occurredAt,
            bidId: null,
            config,
            reservationId);

        var repeatedPairFinding = await CreateRepeatedPairFindingAsync(
            auction.Id.Value,
            sellerId,
            findings,
            occurredAt,
            config,
            cancellationToken);

        if (repeatedPairFinding is not null)
            findings.Add(repeatedPairFinding);

        return findings;
    }

    private static void AddSellerBidderFindings(
        ICollection<AuctionCollusionFinding> findings,
        IReadOnlyCollection<UserSessionEvidence> sessions,
        Guid sellerId,
        Guid bidderId,
        DateTime occurredAt,
        Guid? bidId,
        AuctionCollusionSettings config,
        Guid? reservationId = null)
    {
        if (sellerId == bidderId)
            return;

        var sharedDevices = GetSharedDeviceIds(sessions, sellerId, bidderId, occurredAt - config.SessionDeviceWindow);
        if (sharedDevices.Count > 0)
        {
            findings.Add(new AuctionCollusionFinding(
                AlertType: SellerBidderSameDeviceAlertType,
                SignalType: "seller_bidder_same_device_recent",
                Severity: AlertSeverity.Critical,
                Score: 100,
                SellerId: sellerId,
                ImplicatedUserIds: [sellerId, bidderId],
                BidIds: bidId.HasValue ? [bidId.Value] : [],
                ReservationId: reservationId,
                Window: FormatWindow(config.SessionDeviceWindow),
                EvidenceSummary: "Seller and bidder recently used the same device.",
                PairKeys: [CreateSellerBidderPairKey(sellerId, bidderId)],
                SharedDeviceIds: sharedDevices));
        }

        var sharedIps = GetSharedIpAddresses(sessions, sellerId, bidderId, occurredAt - config.SessionIpWindow);
        if (sharedIps.Count > 0)
        {
            findings.Add(new AuctionCollusionFinding(
                AlertType: SellerBidderSameIpAlertType,
                SignalType: "seller_bidder_same_ip_recent",
                Severity: AlertSeverity.High,
                Score: 85,
                SellerId: sellerId,
                ImplicatedUserIds: [sellerId, bidderId],
                BidIds: bidId.HasValue ? [bidId.Value] : [],
                ReservationId: reservationId,
                Window: FormatWindow(config.SessionIpWindow),
                EvidenceSummary: "Seller and bidder recently used the same IP address.",
                PairKeys: [CreateSellerBidderPairKey(sellerId, bidderId)],
                SharedIpAddresses: sharedIps));
        }
    }

    private static void AddTopBidderFindings(
        ICollection<AuctionCollusionFinding> findings,
        Auction auction,
        IReadOnlyCollection<UserSessionEvidence> sessions,
        Guid sellerId,
        DateTime occurredAt,
        Guid bidId,
        AuctionCollusionSettings config)
    {
        var topBidders = GetTopDistinctBidderIds(auction, 2);
        if (topBidders.Count < 2)
            return;

        var firstBidder = topBidders[0];
        var secondBidder = topBidders[1];
        var pairKey = CreateBidderPairKey(firstBidder, secondBidder);

        var sharedDevices = GetSharedDeviceIds(sessions, firstBidder, secondBidder, occurredAt - config.SessionDeviceWindow);
        if (sharedDevices.Count > 0)
        {
            findings.Add(new AuctionCollusionFinding(
                AlertType: TopBiddersSameDeviceAlertType,
                SignalType: "top_bidders_same_device_recent",
                Severity: AlertSeverity.High,
                Score: 80,
                SellerId: sellerId,
                ImplicatedUserIds: [firstBidder, secondBidder],
                BidIds: [bidId],
                ReservationId: null,
                Window: FormatWindow(config.SessionDeviceWindow),
                EvidenceSummary: "The top two bidders recently used the same device.",
                PairKeys: [pairKey],
                SharedDeviceIds: sharedDevices,
                DominantBidderIds: [firstBidder, secondBidder]));
        }

        var sharedIps = GetSharedIpAddresses(sessions, firstBidder, secondBidder, occurredAt - config.SessionIpWindow);
        var sharedBidIps = GetSharedRecentBidIpAddresses(auction, firstBidder, secondBidder, occurredAt - config.SessionIpWindow);
        if (sharedIps.Count > 0 || sharedBidIps.Count > 0)
        {
            findings.Add(new AuctionCollusionFinding(
                AlertType: TopBiddersSameIpAlertType,
                SignalType: "top_bidders_same_ip_recent",
                Severity: AlertSeverity.Medium,
                Score: 65,
                SellerId: sellerId,
                ImplicatedUserIds: [firstBidder, secondBidder],
                BidIds: [bidId],
                ReservationId: null,
                Window: FormatWindow(config.SessionIpWindow),
                EvidenceSummary: "The top two bidders recently used the same IP address.",
                PairKeys: [pairKey],
                SharedIpAddresses: sharedIps,
                SharedBidIpAddresses: sharedBidIps,
                DominantBidderIds: [firstBidder, secondBidder]));
        }
    }

    private static void AddPingPongFinding(
        ICollection<AuctionCollusionFinding> findings,
        Auction auction,
        Guid sellerId,
        DateTime occurredAt,
        AuctionCollusionSettings config)
    {
        var windowStart = occurredAt - config.PingPongWindow;
        var recentBids = auction.Bids
            .Where(x => x.Status != BidStatus.Cancelled &&
                        !x.IsAutoBid &&
                        x.CreatedAt >= windowStart)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        if (recentBids.Count < config.PingPongMinimumBids)
            return;

        var topGroups = recentBids
            .GroupBy(x => x.BidderId.Value)
            .Select(x => new { BidderId = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .Take(2)
            .ToList();

        if (topGroups.Count < 2)
            return;

        var dominantBidderIds = topGroups.Select(x => x.BidderId).ToArray();
        var dominantCount = topGroups.Sum(x => x.Count);
        var dominanceRatio = dominantCount / (decimal)recentBids.Count;

        if (dominanceRatio < config.PingPongDominanceThreshold)
            return;

        var filtered = recentBids
            .Where(x => dominantBidderIds.Contains(x.BidderId.Value))
            .ToList();

        var alternatingTransitions = 0;
        var totalTransitions = Math.Max(filtered.Count - 1, 1);

        for (var index = 1; index < filtered.Count; index++)
        {
            if (filtered[index - 1].BidderId != filtered[index].BidderId)
                alternatingTransitions++;
        }

        var alternationRatio = alternatingTransitions / (decimal)totalTransitions;
        if (alternationRatio < config.PingPongDominanceThreshold)
            return;

        findings.Add(new AuctionCollusionFinding(
            AlertType: PingPongAlertType,
            SignalType: "ping_pong_bid_ladder",
            Severity: AlertSeverity.Medium,
            Score: 60,
            SellerId: sellerId,
            ImplicatedUserIds: dominantBidderIds,
            BidIds: filtered.Select(x => x.Id.Value).ToArray(),
            ReservationId: null,
            Window: FormatWindow(config.PingPongWindow),
            EvidenceSummary: "Recent bids are dominated by two bidders alternating price increases.",
            PairKeys: [CreateBidderPairKey(dominantBidderIds[0], dominantBidderIds[1])],
            DominancePercent: Math.Round(dominanceRatio * 100m, 2),
            DominantBidderIds: dominantBidderIds,
            SharedBidIpAddresses: filtered
                .Select(x => x.IpAddress?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToArray()));
    }

    private async Task<AuctionCollusionFinding?> CreateRepeatedPairFindingAsync(
        Guid auctionId,
        Guid sellerId,
        IReadOnlyCollection<AuctionCollusionFinding> findings,
        DateTime occurredAt,
        AuctionCollusionSettings config,
        CancellationToken cancellationToken)
    {
        var candidatePairs = findings
            .Where(x => x.Severity == AlertSeverity.High || x.Severity == AlertSeverity.Critical)
            .SelectMany(x => x.PairKeys)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidatePairs.Count == 0)
            return null;

        var matchedPairs = new List<string>();
        var implicatedUsers = new HashSet<Guid>();
        var priorAuctionIds = new HashSet<Guid>();

        foreach (var pairKey in candidatePairs)
        {
            var priorAuctions = await dbContext.Set<MonitoringAlert>()
                .AsNoTracking()
                .Where(x => x.EntityType == "Auction" &&
                            StrongAlertTypes.Contains(x.AlertType) &&
                            x.CreatedAt >= occurredAt - config.RepeatedPairWindow &&
                            x.EntityId != auctionId &&
                            x.Payload.Contains(pairKey))
                .Select(x => x.EntityId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (priorAuctions.Count + 1 < config.RepeatedPairThreshold)
                continue;

            matchedPairs.Add(pairKey);

            foreach (var userId in ParsePairUsers(pairKey))
                implicatedUsers.Add(userId);

            foreach (var priorAuctionId in priorAuctions)
                priorAuctionIds.Add(priorAuctionId);
        }

        if (matchedPairs.Count == 0)
            return null;

        return new AuctionCollusionFinding(
            AlertType: RepeatedPairAlertType,
            SignalType: "repeated_suspicious_pair_recent",
            Severity: AlertSeverity.High,
            Score: 90,
            SellerId: sellerId,
            ImplicatedUserIds: implicatedUsers.ToArray(),
            BidIds: [],
            ReservationId: null,
            Window: FormatWindow(config.RepeatedPairWindow),
            EvidenceSummary: $"The same suspicious pair has triggered strong collusion signals in at least {config.RepeatedPairThreshold} auctions recently.",
            PairKeys: matchedPairs,
            PriorAuctionIds: priorAuctionIds.ToArray());
    }

    private async Task PersistFindingsAsync(
        Guid auctionId,
        IReadOnlyCollection<AuctionCollusionFinding> findings,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        if (findings.Count == 0)
            return;

        var maxWindow = findings
            .Select(x => ParseWindow(x.Window))
            .DefaultIfEmpty(TimeSpan.Zero)
            .Max();

        var openAlerts = await dbContext.Set<MonitoringAlert>()
            .AsNoTracking()
            .Where(x => x.EntityType == "Auction" &&
                        x.EntityId == auctionId &&
                        x.Status == AlertStatus.Open &&
                        x.CreatedAt >= occurredAt - maxWindow)
            .Select(x => new OpenAlertEvidence(x.AlertType, x.CreatedAt))
            .ToListAsync(cancellationToken);

        var implicatedUserIds = findings
            .Where(x => x.Severity == AlertSeverity.High || x.Severity == AlertSeverity.Critical)
            .SelectMany(x => x.ImplicatedUserIds)
            .Distinct()
            .ToList();

        var typedImplicatedUserIds = implicatedUserIds
            .Select(UserId.From)
            .ToArray();

        List<ExistingRiskFlagEvidence> existingRiskFlags;
        if (typedImplicatedUserIds.Length == 0)
        {
            existingRiskFlags = [];
        }
        else
        {
            existingRiskFlags = await dbContext.Set<UserRiskFlag>()
                .AsNoTracking()
                .Where(x => typedImplicatedUserIds.Contains(x.UserId) &&
                            x.FlagType == RiskFlagType &&
                            x.CreatedAt >= occurredAt - maxWindow)
                .Select(x => new ExistingRiskFlagEvidence(x.UserId.Value, x.Reason, x.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        var createdAlerts = new List<MonitoringAlert>();
        var createdAuditLogs = new List<AuditLog>();
        var createdRiskFlags = new List<UserRiskFlag>();

        foreach (var finding in findings)
        {
            var dedupeWindowStart = occurredAt - ParseWindow(finding.Window);
            var hasOpenAlert = openAlerts.Any(x =>
                x.AlertType == finding.AlertType &&
                x.CreatedAt >= dedupeWindowStart);

            if (hasOpenAlert)
                continue;

            var payload = JsonSerializer.Serialize(
                new AuctionCollusionAlertPayload(
                    AuctionId: auctionId,
                    SignalType: finding.SignalType,
                    Score: finding.Score,
                    Severity: finding.Severity.Id,
                    SellerId: finding.SellerId,
                    ImplicatedUserIds: finding.ImplicatedUserIds,
                    BidIds: finding.BidIds,
                    ReservationId: finding.ReservationId,
                    Window: finding.Window,
                    EvidenceSummary: finding.EvidenceSummary,
                    PairKeys: finding.PairKeys,
                    SharedDeviceIds: finding.SharedDeviceIds,
                    SharedIpAddresses: finding.SharedIpAddresses,
                    SharedBidIpAddresses: finding.SharedBidIpAddresses,
                    DominancePercent: finding.DominancePercent,
                    DominantBidderIds: finding.DominantBidderIds,
                    PriorAuctionIds: finding.PriorAuctionIds),
                JsonOptions);

            createdAlerts.Add(MonitoringAlert.Create(
                entityType: "Auction",
                entityId: auctionId,
                alertType: finding.AlertType,
                severity: finding.Severity,
                payload: payload,
                nowUtc: occurredAt));

            createdAuditLogs.Add(AuditLog.Create(
                actorUserId: null,
                actorRole: SystemActorRole,
                action: AuditAction,
                entityType: "Auction",
                entityId: auctionId,
                oldData: null,
                newData: payload,
                ipAddress: null,
                nowUtc: occurredAt));

            openAlerts.Add(new OpenAlertEvidence(finding.AlertType, occurredAt));

            if (finding.Severity != AlertSeverity.High && finding.Severity != AlertSeverity.Critical)
                continue;

            var riskReason = $"Potential auction collusion detected on auction {auctionId} via {finding.SignalType}.";
            var riskSeverity = MapSeverity(finding.Severity);

            foreach (var implicatedUserId in finding.ImplicatedUserIds.Distinct())
            {
                var alreadyFlagged = existingRiskFlags.Any(x =>
                    x.UserId == implicatedUserId &&
                    string.Equals(x.Reason, riskReason, StringComparison.Ordinal) &&
                    x.CreatedAt >= dedupeWindowStart);

                if (alreadyFlagged)
                    continue;

                createdRiskFlags.Add(UserRiskFlag.Create(
                    userId: UserId.From(implicatedUserId),
                    flagType: RiskFlagType,
                    reason: riskReason,
                    severity: riskSeverity,
                    createdBy: null,
                    nowUtc: occurredAt));

                existingRiskFlags.Add(new ExistingRiskFlagEvidence(implicatedUserId, riskReason, occurredAt));
            }
        }

        if (createdAlerts.Count == 0 && createdAuditLogs.Count == 0 && createdRiskFlags.Count == 0)
            return;

        if (createdAlerts.Count > 0)
            dbContext.InsertRange(createdAlerts);

        if (createdAuditLogs.Count > 0)
            dbContext.InsertRange(createdAuditLogs);

        if (createdRiskFlags.Count > 0)
            dbContext.InsertRange(createdRiskFlags);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Detected {AlertCount} auction collusion alert(s) and created {RiskFlagCount} risk flag(s) for auction {AuctionId}.",
            createdAlerts.Count,
            createdRiskFlags.Count,
            auctionId);
    }

    private async Task<Auction?> LoadAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return await dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: query => query
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Item)
                .Include(x => x.Bids)
                .Include(x => x.BuyNowReservations),
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyCollection<UserSessionEvidence>> LoadSessionEvidenceAsync(
        IReadOnlyCollection<Guid> relevantUsers,
        DateTime windowStart,
        CancellationToken cancellationToken)
    {
        if (relevantUsers.Count == 0)
            return [];

        var typedUserIds = relevantUsers
            .Select(UserId.From)
            .ToArray();

        var sessions = await dbContext.Set<UserSession>()
            .AsNoTracking()
            .Where(x => typedUserIds.Contains(x.UserId) &&
                        (x.CreatedAt >= windowStart || x.LastRotatedAt >= windowStart))
            .ToListAsync(cancellationToken);

        return sessions
            .Select(x => new UserSessionEvidence(
                x.UserId.Value,
                x.DeviceId,
                x.IpAddress.ToString(),
                x.UserAgent,
                x.CreatedAt,
                x.LastRotatedAt))
            .ToList();
    }

    private AuctionCollusionSettings LoadSettings()
    {
        var monitoring = runtimeSettings.Monitoring;

        var dominanceThreshold = Math.Clamp(
            monitoring.AuctionCollusionPingPongDominanceThresholdPercent / 100m,
            0.1m,
            1m);

        return new AuctionCollusionSettings(
            SessionDeviceWindow: TimeSpan.FromDays(Math.Max(1, monitoring.AuctionCollusionSessionDeviceWindowDays)),
            SessionIpWindow: TimeSpan.FromDays(Math.Max(1, monitoring.AuctionCollusionSessionIpWindowDays)),
            PingPongWindow: TimeSpan.FromMinutes(Math.Max(1, monitoring.AuctionCollusionPingPongWindowMinutes)),
            PingPongMinimumBids: Math.Max(2, monitoring.AuctionCollusionPingPongMinimumBids),
            PingPongDominanceThreshold: dominanceThreshold,
            RepeatedPairWindow: TimeSpan.FromDays(Math.Max(1, monitoring.AuctionCollusionRepeatedPairWindowDays)),
            RepeatedPairThreshold: Math.Max(2, monitoring.AuctionCollusionRepeatedPairThreshold));
    }

    private static bool ShouldScanAuction(Auction auction, DateTime nowUtc)
    {
        if (auction.Status == AuctionStatus.Active)
            return true;

        return auction.Status == AuctionStatus.Scheduled &&
               auction.Info is not null &&
               auction.Info.HasQualification &&
               auction.Info.IsQualificationOpen(nowUtc);
    }

    private static RiskFlagSeverity MapSeverity(AlertSeverity severity)
    {
        if (severity == AlertSeverity.Critical)
            return RiskFlagSeverity.Critical;

        if (severity == AlertSeverity.High)
            return RiskFlagSeverity.High;

        if (severity == AlertSeverity.Medium)
            return RiskFlagSeverity.Medium;

        return RiskFlagSeverity.Low;
    }

    private static List<Guid> GetTopDistinctBidderIds(Auction auction, int count)
    {
        return auction.Bids
            .Where(x => x.Status != BidStatus.Cancelled)
            .OrderByDescending(x => x.Amount.Amount)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => x.BidderId.Value)
            .Distinct()
            .Take(count)
            .ToList();
    }

    private static List<Guid> GetSharedDeviceIds(
        IReadOnlyCollection<UserSessionEvidence> sessions,
        Guid firstUserId,
        Guid secondUserId,
        DateTime cutoff)
    {
        var firstDevices = sessions
            .Where(x => x.UserId == firstUserId && x.IsRecent(cutoff))
            .Select(x => x.DeviceId)
            .Distinct()
            .ToHashSet();

        var secondDevices = sessions
            .Where(x => x.UserId == secondUserId && x.IsRecent(cutoff))
            .Select(x => x.DeviceId)
            .Distinct();

        return secondDevices
            .Where(firstDevices.Contains)
            .Distinct()
            .ToList();
    }

    private static List<string> GetSharedIpAddresses(
        IReadOnlyCollection<UserSessionEvidence> sessions,
        Guid firstUserId,
        Guid secondUserId,
        DateTime cutoff)
    {
        var firstIps = sessions
            .Where(x => x.UserId == firstUserId &&
                        x.IsRecent(cutoff) &&
                        !string.IsNullOrWhiteSpace(x.IpAddress))
            .Select(x => x.IpAddress)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var secondIps = sessions
            .Where(x => x.UserId == secondUserId &&
                        x.IsRecent(cutoff) &&
                        !string.IsNullOrWhiteSpace(x.IpAddress))
            .Select(x => x.IpAddress)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return secondIps
            .Where(firstIps.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetSharedRecentBidIpAddresses(
        Auction auction,
        Guid firstBidderId,
        Guid secondBidderId,
        DateTime cutoff)
    {
        var firstIps = auction.Bids
            .Where(x => x.Status != BidStatus.Cancelled &&
                        x.BidderId.Value == firstBidderId &&
                        x.CreatedAt >= cutoff &&
                        x.IpAddress is not null)
            .Select(x => x.IpAddress!.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var secondIps = auction.Bids
            .Where(x => x.Status != BidStatus.Cancelled &&
                        x.BidderId.Value == secondBidderId &&
                        x.CreatedAt >= cutoff &&
                        x.IpAddress is not null)
            .Select(x => x.IpAddress!.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return secondIps
            .Where(firstIps.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatWindow(TimeSpan window) => window.ToString("c");

    private static TimeSpan ParseWindow(string window) => TimeSpan.Parse(window);

    private static string CreateSellerBidderPairKey(Guid sellerId, Guid bidderId) =>
        $"seller_bidder:{sellerId:N}:{bidderId:N}";

    private static string CreateBidderPairKey(Guid firstBidderId, Guid secondBidderId)
    {
        var ordered = new[] { firstBidderId, secondBidderId }
            .OrderBy(x => x)
            .ToArray();

        return $"bidder_pair:{ordered[0]:N}:{ordered[1]:N}";
    }

    private static IReadOnlyList<Guid> ParsePairUsers(string pairKey)
    {
        var segments = pairKey.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 3)
            return [];

        var userIds = new List<Guid>(capacity: segments.Length - 1);
        for (var index = 1; index < segments.Length; index++)
        {
            if (Guid.TryParseExact(segments[index], "N", out var parsed))
                userIds.Add(parsed);
        }

        return userIds;
    }

    private sealed record UserSessionEvidence(
        Guid UserId,
        Guid DeviceId,
        string IpAddress,
        string UserAgent,
        DateTime CreatedAt,
        DateTime LastRotatedAt)
    {
        public bool IsRecent(DateTime cutoff) =>
            CreatedAt >= cutoff || LastRotatedAt >= cutoff;
    }

    private sealed record OpenAlertEvidence(
        string AlertType,
        DateTime CreatedAt);

    private sealed record ExistingRiskFlagEvidence(
        Guid UserId,
        string? Reason,
        DateTime CreatedAt);

    private sealed record AuctionCollusionSettings(
        TimeSpan SessionDeviceWindow,
        TimeSpan SessionIpWindow,
        TimeSpan PingPongWindow,
        int PingPongMinimumBids,
        decimal PingPongDominanceThreshold,
        TimeSpan RepeatedPairWindow,
        int RepeatedPairThreshold)
    {
        public DateTime MaxSessionWindowStart(DateTime occurredAt)
        {
            var maxWindow = SessionDeviceWindow > SessionIpWindow
                ? SessionDeviceWindow
                : SessionIpWindow;

            return occurredAt - maxWindow;
        }
    }

    private sealed record AuctionCollusionAlertPayload(
        Guid AuctionId,
        string SignalType,
        int Score,
        string Severity,
        Guid SellerId,
        IReadOnlyList<Guid> ImplicatedUserIds,
        IReadOnlyList<Guid> BidIds,
        Guid? ReservationId,
        string Window,
        string EvidenceSummary,
        IReadOnlyList<string> PairKeys,
        IReadOnlyList<Guid>? SharedDeviceIds,
        IReadOnlyList<string>? SharedIpAddresses,
        IReadOnlyList<string>? SharedBidIpAddresses,
        decimal? DominancePercent,
        IReadOnlyList<Guid>? DominantBidderIds,
        IReadOnlyList<Guid>? PriorAuctionIds);
}
