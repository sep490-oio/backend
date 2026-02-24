using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Item
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }

    public Guid? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Condition { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? Quantity { get; set; }

    public string? Attributes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<ItemImage> ItemImages { get; set; } = new List<ItemImage>();

    public virtual ICollection<ItemQuestion> ItemQuestions { get; set; } = new List<ItemQuestion>();

    public virtual User Seller { get; set; } = null!;
}
