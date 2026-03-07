using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => RoleId.From(value));

        builder.Property(r => r.RoleName)
            .HasColumnName("role_name")
            .HasMaxLength(150)
            .IsRequired();
        
        builder.Property(r => r.Level)
            .HasColumnName("level")
            .IsRequired();
        
        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(r => r.NormalizedRoleName)
            .HasColumnName("normalized_role_name")
            .HasMaxLength(150)
            .HasComputedColumnSql("UPPER(role_name)", stored: true);

        builder.HasIndex(r => r.NormalizedRoleName)
            .IsUnique();

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Ignore(r => r.DomainEvents);
    }
}