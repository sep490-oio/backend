using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

public sealed class AuctionEmergencyActionConfiguration : IEntityTypeConfiguration<AuctionEmergencyAction>
{
    public void Configure(EntityTypeBuilder<AuctionEmergencyAction> builder)
    {
        builder.ToTable("auction_emergency_actions");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => AuctionEmergencyActionId.From(x))
            .IsRequired();
        
        builder.Property(a => a.EmergencyId)
            .HasColumnName("emergency_id")
            .HasConversion(x => x.Value, x => AuctionEmergencyId.From(x))
            .IsRequired();
        
        builder.Property(a => a.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.HasIndex(a => a.EmergencyId)
            .HasDatabaseName("idx_auction_emergency_actions_emergency_id");
    }
}