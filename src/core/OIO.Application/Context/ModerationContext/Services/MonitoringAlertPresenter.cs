using System.Text.Json;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.Context.ModerationContext.Aggregates;

namespace OIO.Application.Context.ModerationContext.Services;

internal static class MonitoringAlertPresenter
{
    public static MonitoringAlertDto ToDto(MonitoringAlert alert)
    {
        var payload = ParsePayload(alert.Payload);
        var alertType = alert.AlertType.ToLowerInvariant();
        var score = PickNumber(payload, "score", "riskScore", "collusionScore");
        var windowLabel = FormatWindow(PickString(payload, "window", "windowLabel", "timeWindow"));
        var evidenceSummary = PickString(payload, "evidenceSummary", "summary", "message", "reason", "detail");
        var title = FormatAlertTitle(alert.AlertType);
        var summary = BuildSummary(alertType, evidenceSummary, score, windowLabel);
        var participants = BuildParticipants(payload);
        var evidenceRefs = BuildEvidenceRefs(alert, payload);
        var nowUtc = DateTime.UtcNow;

        return new MonitoringAlertDto(
            Id: alert.Id.Value,
            EntityType: alert.EntityType,
            EntityId: alert.EntityId,
            AlertType: alert.AlertType,
            Severity: alert.Severity.Id,
            Payload: alert.Payload,
            Status: alert.Status.Id,
            Notes: alert.Notes,
            AcknowledgedBy: alert.AcknowledgedBy,
            AcknowledgedAt: alert.AcknowledgedAt,
            ResolvedBy: alert.ResolvedBy,
            ResolvedAt: alert.ResolvedAt,
            CreatedAt: alert.CreatedAt,
            AlertTitle: title,
            Score: score,
            Summary: summary,
            AgeSeconds: Math.Max(0, Convert.ToInt64((nowUtc - alert.CreatedAt).TotalSeconds)),
            PrimaryEntity: new MonitoringPrimaryEntityDto(
                alert.EntityType,
                alert.EntityId,
                null,
                RouteFor(alert.EntityType, alert.EntityId.ToString())),
            Participants: participants,
            EvidenceRefs: evidenceRefs,
            WindowLabel: windowLabel,
            RecommendedNextStep: RecommendedNextStep(alertType),
            RawPayloadAvailable: !string.IsNullOrWhiteSpace(alert.Payload) && alert.Payload.Trim() != "{}",
            AssignedTo: alert.AssignedTo,
            AssignedAt: alert.AssignedAt,
            SlaDueAt: alert.SlaDueAt,
            IsOverdue: alert.SlaDueAt.HasValue && alert.SlaDueAt.Value < nowUtc && alert.Status.Id is "open" or "acknowledged",
            ResolutionOutcome: alert.ResolutionOutcome,
            ResolutionReason: alert.ResolutionReason,
            Fingerprint: alert.Fingerprint);
    }

    private static Dictionary<string, JsonElement> ParsePayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return [];

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            return document.RootElement
                .EnumerateObject()
                .ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string FormatAlertTitle(string alertType)
    {
        var type = alertType.ToLowerInvariant();
        if (type.Contains("seller_bidder_same_device")) return "Seller and bidder used same device";
        if (type.Contains("seller_bidder_same_ip")) return "Seller and bidder used same IP";
        if (type.Contains("top_bidders_same_device")) return "Top bidders used same device";
        if (type.Contains("top_bidders_same_ip")) return "Top bidders used same IP";
        if (type.Contains("ping_pong")) return "Ping-pong bidding pattern";
        if (type.Contains("repeated_pair")) return "Repeated suspicious seller/bidder pair";
        if (type.Contains("non_payment") || type.Contains("payment_defaulted")) return "Repeated non-payment";
        if (type.Contains("terminated")) return "Auction terminated";
        if (type.Contains("buyer_reported_damage")) return "Buyer reported item damage";

        return string.Join(' ', alertType
            .Replace('-', '_')
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Capitalize));
    }

    private static string BuildSummary(string alertType, string? evidenceSummary, int? score, string? windowLabel)
    {
        if (!string.IsNullOrWhiteSpace(evidenceSummary))
            return evidenceSummary;

        var scoreText = score.HasValue ? $" Risk score: {score.Value}." : string.Empty;
        var windowText = !string.IsNullOrWhiteSpace(windowLabel) ? $" Detection window: {windowLabel}." : string.Empty;

        if (alertType.Contains("same_device")) return $"Users share device evidence.{windowText}{scoreText}";
        if (alertType.Contains("same_ip")) return $"Users share IP evidence.{windowText}{scoreText}";
        if (alertType.Contains("repeated_pair")) return $"The same suspicious pair appears across auctions.{windowText}{scoreText}";
        if (alertType.Contains("non_payment") || alertType.Contains("payment_defaulted")) return $"User has a non-payment risk signal.{windowText}{scoreText}";

        return "No additional structured detail.";
    }

    private static IReadOnlyList<MonitoringParticipantDto> BuildParticipants(Dictionary<string, JsonElement> payload)
    {
        var participants = new List<MonitoringParticipantDto>();
        AddParticipant(participants, "seller", PickGuid(payload, "sellerId"));
        AddParticipant(participants, "bidder", PickGuid(payload, "bidderId"));
        AddParticipant(participants, "buyer", PickGuid(payload, "buyerId"));
        AddParticipant(participants, "winner", PickGuid(payload, "winnerId"));

        foreach (var userId in PickGuidList(payload, "implicatedUserIds", "userIds", "bidderIds"))
            AddParticipant(participants, "implicated_user", userId);

        return participants
            .GroupBy(x => x.UserId)
            .Select(x => x.First())
            .ToArray();
    }

    private static IReadOnlyList<MonitoringEvidenceRefDto> BuildEvidenceRefs(
        MonitoringAlert alert,
        Dictionary<string, JsonElement> payload)
    {
        var refs = new List<MonitoringEvidenceRefDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRef(
            refs,
            seen,
            alert.EntityType,
            alert.EntityId.ToString(),
            $"Primary {FormatEntityLabel(alert.EntityType)} ID",
            "primary_entity",
            "Main object the alert is about.");
        AddRef(refs, seen, "auction", PickGuid(payload, "auctionId")?.ToString(), "Related auction ID", "related", "Auction connected to this alert.");
        AddRef(refs, seen, "user", PickGuid(payload, "sellerId")?.ToString(), "Seller user ID", "participants", "Seller involved in this alert.");
        AddRef(refs, seen, "user", PickGuid(payload, "bidderId")?.ToString(), "Bidder user ID", "participants", "Bidder involved in this alert.");
        AddRef(refs, seen, "user", PickGuid(payload, "buyerId")?.ToString(), "Buyer user ID", "participants", "Buyer involved in this alert.");
        AddRef(refs, seen, "user", PickGuid(payload, "winnerId")?.ToString(), "Winner user ID", "participants", "Winner involved in this alert.");

        foreach (var id in PickGuidList(payload, "implicatedUserIds", "userIds", "bidderIds"))
            AddRef(refs, seen, "user", id.ToString(), "Implicated user ID", "participants", "User involved in this alert.");

        foreach (var id in PickGuidList(payload, "bidIds"))
            AddRef(refs, seen, "bid", id.ToString(), "Suspicious bid ID", "evidence", "Bid record used by the detection rule.");

        foreach (var id in PickGuidList(payload, "sharedDeviceIds", "deviceIds"))
            AddRef(refs, seen, "device", id.ToString(), "Device fingerprint", "evidence", "Device fingerprint used by the detection rule.");

        foreach (var ip in PickStringList(payload, "sharedIpAddresses", "sharedBidIpAddresses", "ip", "ipAddress"))
            AddRef(refs, seen, "ip", ip, "Shared IP", "evidence", "IP address used by the detection rule.");

        foreach (var id in PickGuidList(payload, "priorAuctionIds", "auctionIds"))
            AddRef(refs, seen, "auction", id.ToString(), "Prior auction ID", "related", "Prior auction connected to this signal.");

        foreach (var id in PickGuidList(payload, "orderIds", "orderId"))
            AddRef(refs, seen, "order", id.ToString(), "Related order ID", "related", "Order connected to this alert.");

        return refs;
    }

    private static void AddParticipant(List<MonitoringParticipantDto> participants, string role, Guid? userId)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
            participants.Add(new MonitoringParticipantDto(role, userId.Value, null));
    }

    private static void AddRef(
        List<MonitoringEvidenceRefDto> refs,
        HashSet<string> seen,
        string type,
        string? value,
        string label,
        string group,
        string description)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var key = $"{type}:{value}";
        if (!seen.Add(key))
            return;

        refs.Add(new MonitoringEvidenceRefDto(
            type,
            value,
            label,
            RouteFor(type, value),
            value,
            group,
            description));
    }

    private static string? RouteFor(string type, string value)
    {
        return type.ToLowerInvariant() switch
        {
            "auction" => $"/admin/auctions/{value}",
            "user" => $"/admin/users/{value}",
            "order" => $"/admin/orders/{value}",
            _ => null
        };
    }

    private static string RecommendedNextStep(string alertType)
    {
        if (alertType.Contains("same_ip")) return "Review bids and sessions from the shared IP, then open the auction bid history.";
        if (alertType.Contains("same_device")) return "Review the shared device fingerprint and compare seller/bidder behavior.";
        if (alertType.Contains("repeated_pair")) return "Review prior auctions involving the same pair before resolving.";
        if (alertType.Contains("non_payment") || alertType.Contains("payment_defaulted")) return "Review the user's payment defaults and affected orders.";
        return "Open the related entity and compare evidence with the timeline before resolving.";
    }

    private static int? PickNumber(Dictionary<string, JsonElement> payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!payload.TryGetValue(key, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                return parsed;
        }

        return null;
    }

    private static string? PickString(Dictionary<string, JsonElement> payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!payload.TryGetValue(key, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString();

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                return value.ToString();
        }

        return null;
    }

    private static Guid? PickGuid(Dictionary<string, JsonElement> payload, params string[] keys)
    {
        var value = PickString(payload, keys);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static IReadOnlyList<Guid> PickGuidList(Dictionary<string, JsonElement> payload, params string[] keys)
    {
        var values = new List<Guid>();

        foreach (var key in keys)
        {
            if (!payload.TryGetValue(key, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.Array)
            {
                values.AddRange(element.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                    .Where(x => Guid.TryParse(x, out _))
                    .Select(x => Guid.Parse(x!)));
                continue;
            }

            var single = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
            if (Guid.TryParse(single, out var parsed))
                values.Add(parsed);
        }

        return values.Distinct().ToArray();
    }

    private static IReadOnlyList<string> PickStringList(Dictionary<string, JsonElement> payload, params string[] keys)
    {
        var values = new List<string>();

        foreach (var key in keys)
        {
            if (!payload.TryGetValue(key, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.Array)
            {
                values.AddRange(element.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>());
                continue;
            }

            var single = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
            if (!string.IsNullOrWhiteSpace(single))
                values.Add(single);
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string? FormatWindow(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!TimeSpan.TryParse(raw, out var timeSpan))
            return raw;

        if (timeSpan.TotalDays >= 1)
            return $"{Math.Round(timeSpan.TotalDays)} days";

        if (timeSpan.TotalHours >= 1)
            return $"{Math.Round(timeSpan.TotalHours)} hours";

        return $"{Math.Max(1, Math.Round(timeSpan.TotalMinutes))} minutes";
    }

    private static string FormatEntityLabel(string entityType)
    {
        return string.Join(' ', entityType
            .Replace('-', '_')
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Capitalize));
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return string.Concat(value[0].ToString().ToUpperInvariant(), value.AsSpan(1).ToString());
    }
}
