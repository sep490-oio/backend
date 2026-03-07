using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => PermissionId.From(value));

        builder.Property(p => p.PermissionCode)
            .HasColumnName("permission_code")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.NormalizedPermissionCode)
            .HasColumnName("normalized_permission_code")
            .HasMaxLength(150)
            .HasComputedColumnSql("UPPER(permission_code)", stored: true);

        builder.HasIndex(p => p.NormalizedPermissionCode)
            .IsUnique();
        
    }
}