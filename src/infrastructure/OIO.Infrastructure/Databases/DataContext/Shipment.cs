using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Shipment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public string? ShippingMethod { get; set; }

    public DateOnly? EstimatedDelivery { get; set; }

    public DateTime? ActualDelivery { get; set; }

    public string Status { get; set; } = null!;

    public decimal? ShippingFee { get; set; }

    public decimal? WeightKg { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
