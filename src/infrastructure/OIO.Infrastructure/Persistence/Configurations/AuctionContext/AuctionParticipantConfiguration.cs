using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

public sealed class AuctionParticipantConfiguration : IEntityTypeConfiguration<AuctionParticipant>
{
    public void Configure(EntityTypeBuilder<AuctionParticipant> builder)
    {
        builder.ToTable("auction_participants");
        
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => AuctionParticipantId.From(x));
        
        builder.Property(p => p.AuctionId)
            .HasColumnName("auction_id")
            .HasConversion(x => x.Value, x => AuctionId.From(x))
            .IsRequired();
        
        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .HasConversion(x => x.Value, x => UserId.From(x))
            .IsRequired();
        
        builder.Property(p => p.RoleInAuction)
            .HasColumnName("role_in_auction")
            .HasMaxLength(255)
            .IsRequired();

        builder.ComplexProperty(p => p.JoinStatus, joinStatusBuilder =>
        {
            joinStatusBuilder.Property(s => s.Id)
                .HasColumnName("join_status")
                .IsRequired();
        });

        builder.ComplexProperty(p => p.QualificationStatus, qualificationStatusBuilder =>
            {
                qualificationStatusBuilder.Property(s => s.Id)
                    .HasColumnName("qualification_status")
                    .IsRequired();
            })
            .HasComplexCompositeIndex(p => new { p.AuctionId, p.QualificationStatus.Id },
                 indexName: "idx_auction_participants_qualification_status");
        
        builder.Property(p => p.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(p => p.QualifiedAt)
            .HasColumnName("qualified_at");

        builder.Property(p => p.RejectedReason)
            .HasColumnName("rejected_reason");
        
        builder.HasIndex(p => new { p.AuctionId, p.UserId })
            .HasDatabaseName("idx_auction_participants_auction_user")
            .IsUnique();
        
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_auction_participants_user_id");
        
        builder.HasIndex(p => p.AuctionId)
            .HasDatabaseName("idx_auction_participants_auction_id");
    }
}