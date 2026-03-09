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

        builder.HasKey(r => r.Name);

        builder.Property(r => r.Name)
            .ValueGeneratedNever()
            .HasColumnName("name");
        
        builder.Property(r => r.Level)
            .HasColumnName("level")
            .IsRequired();
        
        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleName)
            .OnDelete(DeleteBehavior.Cascade);
    }
}