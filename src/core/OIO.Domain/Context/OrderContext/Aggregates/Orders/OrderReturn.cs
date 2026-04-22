using CSharpFunctionalExtensions;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders;

public sealed class OrderReturn : BaseEntity<OrderReturnId>
{
    private readonly List<OrderReturnEvidence> _evidence = new();

    public OrderId OrderId { get; private set; }
    public UserId BuyerId { get; private set; }
    public string ReasonCode { get; private set; }
    public string? Description { get; private set; }
    public OrderReturnStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? DecisionReason { get; private set; }
    public string? ProviderCode { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public DateTime? SellerReceivedAt { get; private set; }
    public DateTime? SellerConfirmedReceivedAt { get; private set; }
    public DateTime? BuyerDecisionDueAt { get; private set; }

    /// <summary>
    /// Who pays the return-shipping fee. Set by <see cref="Order.OpenReturnViaDispute"/>
    /// when the dispute-resolution flow opens a pre-approved return.
    /// Internal set — only writable from within this namespace (the Order aggregate).
    /// </summary>
    public ShippingFeePayer? ShippingFeePayer { get; internal set; }

    /// <summary>
    /// Refund intent recorded at dispute-resolve time. When non-null and non-None, the
    /// refund is deferred to <c>ConfirmOrderReturnReceivedCommandHandler</c> — i.e. the
    /// refund only fires after the seller receives the goods back and photos are posted.
    /// Set via <see cref="Order.OpenReturnViaDispute"/> (internal set — same namespace).
    /// </summary>
    public DeferredRefundIntent? DeferredRefundIntent { get; internal set; }

    /// <summary>
    /// Amount for a <see cref="Enums.DeferredRefundIntent.Partial"/> refund. Must be
    /// &gt; 0 when intent is Partial; null otherwise. Internal set — Order aggregate only.
    /// </summary>
    public decimal? DeferredRefundAmount { get; internal set; }

    /// <summary>
    /// Signed return-scoped QR token issued at <see cref="Approve"/> time so the
    /// buyer can print the shipping label BEFORE handing the parcel to the carrier,
    /// and the seller can scan the parcel on arrival. Distinct from outbound QR —
    /// see <c>IReturnShipmentQrTokenService</c>. Null until the return is approved.
    /// </summary>
    public string? QrToken { get; private set; }

    /// <summary>
    /// Last time a buyer-decision reminder was sent for this return.
    /// Used by <c>OrderReturnReminderJob</c> (Quartz) to prevent cross-job overlap:
    /// the job checks + sets this field before sending so hourly runs don't double-send.
    /// </summary>
    public DateTime? LastReminderSentAt { get; private set; }

    /// <summary>
    /// Stamps <see cref="LastReminderSentAt"/>. Called by
    /// <c>OrderReturnReminderJob</c> after it has successfully dispatched the
    /// buyer-reminder notification so the next hourly run skips this row.
    /// </summary>
    public void MarkReminderSent(DateTime nowUtc)
    {
        LastReminderSentAt = nowUtc;
    }

    public Order Order { get; private set; } = null!;

    public IReadOnlyCollection<OrderReturnEvidence> Evidence => _evidence.AsReadOnly();

    /// <summary>True once at least one <see cref="OrderReturnEvidenceCategory.PickupByBuyer"/> photo exists.</summary>
    public bool HasPickupEvidence =>
        _evidence.Any(e => e.Category == OrderReturnEvidenceCategory.PickupByBuyer.Id);

    /// <summary>True once at least one <see cref="OrderReturnEvidenceCategory.ReceiptBySeller"/> photo exists.</summary>
    public bool HasReceiptEvidence =>
        _evidence.Any(e => e.Category == OrderReturnEvidenceCategory.ReceiptBySeller.Id);

    private OrderReturn() { }

    public static OrderReturn Create(
        OrderId orderId,
        UserId buyerId,
        string reasonCode,
        string? description,
        DateTime buyerDecisionDueAt,
        DateTime nowUtc)
    {
        return new OrderReturn
        {
            Id = OrderReturnId.From(Guid.CreateVersion7()),
            OrderId = orderId,
            BuyerId = buyerId,
            ReasonCode = reasonCode,
            Description = description,
            Status = OrderReturnStatus.Requested,
            RequestedAt = nowUtc,
            BuyerDecisionDueAt = buyerDecisionDueAt,
            DeferredRefundIntent = Enums.DeferredRefundIntent.None
        };
    }

    public UnitResult<Error> Approve(string? reason, DateTime nowUtc, string? qrToken = null)
    {
        if (Status != OrderReturnStatus.Requested)
            return Error.Conflict("OrderReturn.InvalidState", "Return request cannot be approved.");

        Status = OrderReturnStatus.Approved;
        ApprovedAt = nowUtc;
        DecisionReason = reason;

        // Stamp the QR token at approval time so the buyer can print the
        // shipping label BEFORE handing the parcel to the carrier. Optional
        // to preserve call-site back-compat — callers that don't yet mint a
        // token (legacy tests) still hit Approve() without breakage.
        if (!string.IsNullOrWhiteSpace(qrToken))
            QrToken = qrToken;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(string reason, DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.Requested)
            return Error.Conflict("OrderReturn.InvalidState", "Return request cannot be rejected.");

        Status = OrderReturnStatus.Rejected;
        RejectedAt = nowUtc;
        DecisionReason = reason;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkReturnShipped(
        string providerCode,
        string trackingNumber,
        DateTime shippedAt,
        DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.Approved)
            return Error.Conflict("OrderReturn.InvalidState", "Return shipment can only be created after approval.");

        // Evidence guard — at least one PickupByBuyer photo required before shipping.
        if (!HasPickupEvidence)
            return Error.Validation(
                "evidence",
                "OrderReturn.EvidenceRequired",
                "At least one pickup photo required before marking shipped.");

        ProviderCode = providerCode;
        TrackingNumber = trackingNumber;
        ShippedAt = shippedAt;
        Status = OrderReturnStatus.ReturnInTransit;
        ReturnedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkSellerReceived(DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.ReturnInTransit && Status != OrderReturnStatus.Approved)
            return Error.Conflict("OrderReturn.InvalidState", "Seller can only confirm a return after the return is approved and in transit.");

        // D3: NO evidence guard here — scan is identity-proof only. Receipt-evidence
        // enforcement lives on Resolve() so the seller can scan first and upload later.
        Status = OrderReturnStatus.SellerReceived;
        SellerReceivedAt = nowUtc;
        SellerConfirmedReceivedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resolve(DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.SellerReceived && Status != OrderReturnStatus.Rejected)
            return Error.Conflict("OrderReturn.InvalidState", "Return can only be resolved after seller confirmation or rejection.");

        // D3: receipt-evidence guard fires at resolve. Skipped for Rejected terminal
        // path (rejected returns never acquire receipt evidence — no custody change).
        if (Status == OrderReturnStatus.SellerReceived && !HasReceiptEvidence)
            return Error.Validation(
                "evidence",
                "OrderReturn.EvidenceRequired",
                "At least one receipt photo required before resolve.");

        Status = OrderReturnStatus.Resolved;
        ReturnedAt ??= nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(string? reason, DateTime nowUtc)
    {
        if (Status == OrderReturnStatus.Resolved || Status == OrderReturnStatus.Cancelled)
            return Error.Conflict("OrderReturn.InvalidState", "Return is already terminal.");

        Status = OrderReturnStatus.Cancelled;
        DecisionReason = reason;
        ReturnedAt ??= nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Attaches an evidence photo to this return. Silent — does NOT raise a domain
    /// event per Principle 5 (OrderReturn is BaseEntity, not AggregateRoot). Caller
    /// must confirm + link the MediaUpload upstream.
    /// </summary>
    /// <remarks>
    /// Allowed status windows:
    /// <list type="bullet">
    /// <item><see cref="OrderReturnEvidenceCategory.PickupByBuyer"/>: <see cref="OrderReturnStatus.Approved"/> only.</item>
    /// <item><see cref="OrderReturnEvidenceCategory.ReceiptBySeller"/>: <see cref="OrderReturnStatus.ReturnInTransit"/> or <see cref="OrderReturnStatus.SellerReceived"/>.</item>
    /// </list>
    /// </remarks>
    public Result<OrderReturnEvidence, Error> AddEvidence(
        DateTime nowUtc,
        OrderReturnEvidenceCategory category,
        MediaUpload upload,
        UserId createdBy)
    {
        if (category == OrderReturnEvidenceCategory.PickupByBuyer)
        {
            if (Status != OrderReturnStatus.Approved)
                return Error.Conflict(
                    "OrderReturn.EvidenceNotAllowed",
                    $"Pickup evidence is only allowed in Approved status, but return is '{Status.Id}'.");
        }
        else if (category == OrderReturnEvidenceCategory.ReceiptBySeller)
        {
            if (Status != OrderReturnStatus.ReturnInTransit && Status != OrderReturnStatus.SellerReceived)
                return Error.Conflict(
                    "OrderReturn.EvidenceNotAllowed",
                    $"Receipt evidence is only allowed in ReturnInTransit or SellerReceived, but return is '{Status.Id}'.");
        }
        else
        {
            return Error.Validation(
                "category",
                "OrderReturn.UnknownEvidenceCategory",
                $"Unknown evidence category '{category.Id}'.");
        }

        var evidence = new OrderReturnEvidence(
            OrderReturnEvidenceId.From(Guid.CreateVersion7()),
            Id,
            category.Id,
            upload.Id,
            upload.Info.SecureUrl,
            upload.Info.FileName,
            upload.ResourceType,
            DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            createdBy);

        _evidence.Add(evidence);
        return evidence;
    }

    /// <summary>
    /// Stamps the signed return-scoped QR token onto this return. Silent — no event
    /// (Principle 5). Now issuable at <see cref="OrderReturnStatus.Approved"/> so the
    /// buyer can print the shipping label BEFORE handing off to the carrier, and
    /// still re-issuable in <see cref="OrderReturnStatus.ReturnInTransit"/> for the
    /// legacy MarkReturnShipped call site.
    /// </summary>
    public UnitResult<Error> IssueReturnQr(string qrToken, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
            return Error.Validation("qrToken", "OrderReturn.InvalidQrToken", "QR token is required.");

        if (Status != OrderReturnStatus.Approved && Status != OrderReturnStatus.ReturnInTransit)
            return Error.Conflict(
                "OrderReturn.InvalidState",
                $"QR token can only be issued in Approved or ReturnInTransit status, but return is '{Status.Id}'.");

        QrToken = qrToken;
        return UnitResult.Success<Error>();
    }
}
