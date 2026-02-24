using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserAddress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = null!;

    public string RecipientName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string District { get; set; } = null!;

    public string City { get; set; } = null!;

    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<Order> OrderBillingAddresses { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderShippingAddressNavigations { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
