using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");

        // Composite primary key
        builder.HasKey(up => new { up.UserId, up.PermissionCode });

        builder.Property(up => up.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(up => up.PermissionCode)
            .HasColumnName("permission_code")
            .IsRequired();

        builder.Property(up => up.IsAllowed)
            .HasColumnName("is_allowed")
            .HasDefaultValue(true)
            .IsRequired();
    }
}