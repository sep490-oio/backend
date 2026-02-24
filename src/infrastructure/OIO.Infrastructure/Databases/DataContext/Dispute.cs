using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Dispute
{
    public Guid Id { get; set; }

    public string DisputeNumber { get; set; } = null!;

    public Guid OrderId { get; set; }

    public Guid AuctionId { get; set; }

    public Guid ComplainantId { get; set; }

    public Guid RespondentId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? DesiredResolution { get; set; }

    public string Status { get; set; } = null!;

    public string? Priority { get; set; }

    public string? ResolutionType { get; set; }

    public string? ResolutionNotes { get; set; }

    public decimal? ResolutionAmount { get; set; }

    public Guid? AssignedTo { get; set; }

    public Guid? EscalatedTo { get; set; }

    public DateTime? ResponseDeadline { get; set; }

    public DateTime? EscalatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User Complainant { get; set; } = null!;

    public virtual ICollection<DisputeEvidence> DisputeEvidences { get; set; } = new List<DisputeEvidence>();

    public virtual ICollection<DisputeMessage> DisputeMessages { get; set; } = new List<DisputeMessage>();

    public virtual ICollection<DisputeRefund> DisputeRefunds { get; set; } = new List<DisputeRefund>();

    public virtual ICollection<DisputeStatusHistory> DisputeStatusHistories { get; set; } = new List<DisputeStatusHistory>();

    public virtual User? EscalatedToNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User Respondent { get; set; } = null!;
}
