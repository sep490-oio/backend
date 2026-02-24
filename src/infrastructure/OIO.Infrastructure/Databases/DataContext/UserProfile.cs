using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserProfile
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User IdNavigation { get; set; } = null!;
}
