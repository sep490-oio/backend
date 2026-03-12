using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

public class AuctionEmergencyConfiguration : IEntityTypeConfiguration<AuctionEmergency>
{
    public void Configure(EntityTypeBuilder<AuctionEmergency> builder)
    {
        builder.ToTable("auction_emergencies");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => AuctionEmergencyId.From(x))
            .IsRequired();

        builder.Property(a => a.AuctionId)
            .HasColumnName("auction_id")
            .HasConversion(x => x.Value, x => AuctionId.From(x))
            .IsRequired();
        
        builder.Property(a => a.TriggeredById)
            .HasColumnName("triggered_by")
            .HasConversion(x => x.HasValue ? x.Value.Value : default(Guid?), x => x.HasValue ? UserId.From(x.Value) : null);
        
        builder.Property(a => a.TriggerSource)
            .HasColumnName("trigger_source")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(a => a.Reason)
            .HasColumnName("reason")
            .HasMaxLength(255)
            .IsRequired();

        builder.ComplexProperty(a => a.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .IsRequired()
                .HasComplexIndex(indexName: "idx_auction_emergencies_status");
        });
        
        builder.Property(a => a.TriggeredAt)
            .HasColumnName("triggered_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.HasOne(d => d.TriggerBy)
            .WithMany(p => p.Emergencies)
            .HasForeignKey(d => d.TriggeredById)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("auction_emergencies_triggered_by_fkey");
        
        builder.HasIndex(a => a.AuctionId)
            .HasDatabaseName("idx_auction_emergencies_auction_id");
    }
}