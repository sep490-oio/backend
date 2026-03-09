using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.Shared;

internal sealed class SystemSettingConfiguration
    : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("Id")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(x => x.Value, x => SystemSettingId.From(x));

        builder.Property(s => s.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(s => s.ValueType)
            .HasColumnName("value_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(s => s.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);
    }
}