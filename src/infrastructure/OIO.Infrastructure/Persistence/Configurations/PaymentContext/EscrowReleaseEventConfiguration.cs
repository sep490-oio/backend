using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class EscrowReleaseEventConfiguration : IEntityTypeConfiguration<EscrowReleaseEvent>
{
    public void Configure(EntityTypeBuilder<EscrowReleaseEvent> builder)
    {
        builder.ToTable("escrow_release_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EscrowId)
            .HasColumnName("escrow_id")
            .IsRequired();

        builder.ComplexProperty(e => e.ReleaseType, rtBuilder =>
        {
            rtBuilder.Property(r => r.Id)
                .HasColumnName("release_type")
                .HasMaxLength(30)
                .IsRequired();
        });

        builder.Property(e => e.TriggerSourceType)
            .HasColumnName("trigger_source_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TriggerSourceId)
            .HasColumnName("trigger_source_id");

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.EscrowId)
            .HasDatabaseName("idx_escrow_release_events_escrow");

        builder.HasIndex(e => new { e.TriggerSourceType, e.TriggerSourceId })
            .HasDatabaseName("idx_escrow_release_events_trigger");
    }
}
