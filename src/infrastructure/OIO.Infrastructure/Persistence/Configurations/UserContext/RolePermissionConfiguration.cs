using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleName, rp.PermissionCode });

        builder.Property(rp => rp.RoleName)
            .HasColumnName("role_name")
            .IsRequired();
        
        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(rp => rp.PermissionCode)
            .HasColumnName("permission_code")
            .IsRequired();

        builder.Property(rp => rp.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}