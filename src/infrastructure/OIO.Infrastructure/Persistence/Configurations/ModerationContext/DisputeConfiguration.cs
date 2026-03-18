using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Infrastructure.Persistence.Converters;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");
        
        builder.HasKey(d => d.Id);

        builder.ComplexProperty(d => d.DisputeNumber, disputeNumber =>
        {
                disputeNumber.Property(dn => dn.Value)
                    .HasColumnName("dispute_number")
                    .IsRequired()
                    .HasComplexIndex(isUnique: true);
        });
        
        builder.Property(d => d.OrderId)
            .HasColumnName("order_id")
            .IsRequired();
        
        builder.Property(d => d.AuctionId)
            .HasColumnName("auction_id");

        builder.Property(d => d.VerificationId)
            .HasColumnName("verification_id");
        
        builder.Property(d => d.ComplainantId)
            .HasColumnName("complainant_id")
            .IsRequired();
        
        builder.Property(d => d.RespondentId)
            .HasColumnName("respondent_id")
            .IsRequired();

        builder.ComplexProperty(d => d.Type, typeBuilder =>
        {
            typeBuilder.Property(t => t.Id)
                .HasColumnName("type")
                .IsRequired();
        });
        
        builder.Property(d => d.Title)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .IsRequired();
        
        builder.ComplexProperty(d => d.DesiredResolution, desiredResolutionBuilder =>
        {
            desiredResolutionBuilder.Property(dr => dr.Id)
                .HasColumnName("desired_resolution");
        });
        
        builder.ComplexProperty(d => d.Status, statusBuilder =>
        {
            statusBuilder.Property(dr => dr.Id)
                .HasColumnName("status")
                .HasComplexIndex();
        });
        
        builder.ComplexProperty(d => d.Priority, priorityBuilder =>
        {
            priorityBuilder.Property(dr => dr.Id)
                .HasColumnName("priority");
        });
        
        builder.ComplexProperty(d => d.ResolutionType, resolutionTypeBuilder =>
        {
            resolutionTypeBuilder.Property(rt => rt.Id)
                .HasColumnName("resolution_type");
        });
        
        builder.Property(d => d.ResolutionNotes)
            .HasColumnName("resolution_notes");
        
        builder.Property(d => d.ResolutionAmount)
            .HasColumnName("resolution_amount")
            .HasColumnType("decimal(18,2)");
        
        builder.Property(d => d.AssignedTo)
            .HasColumnName("assigned_to");
        
        builder.Property(d => d.EscalatedTo)
            .HasColumnName("escalated_to");
        
        builder.Property(d => d.ResponseDeadline)
            .HasColumnName("response_deadline");
        
        builder.Property(d => d.EscalatedAt)
            .HasColumnName("escalated_at");
        
        builder.Property(d => d.ResolvedAt)
            .HasColumnName("resolved_at");
        
        builder.Property(d => d.ClosedAt)
            .HasColumnName("closed_at");
        
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(d => d.ModifiedAt)
            .HasColumnName("modified_at");
        
        //Navigation
        builder.HasMany(d => d.Evidence)
            .WithOne(e => e.Dispute)
            .HasForeignKey(e => e.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(d => d.Messages)
            .WithOne(m => m.Dispute)
            .HasForeignKey(m => m.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(d => d.Refunds)
            .WithOne(r => r.Dispute)
            .HasForeignKey(r => r.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(d => d.StatusHistory)
            .WithOne(sh => sh.Dispute)
            .HasForeignKey(sh => sh.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<IdentityVerification>()
            .WithMany()
            .HasForeignKey(d => d.VerificationId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(d => d.OrderId)
            .HasDatabaseName("idx_disputes_order_id");

        builder.HasIndex(d => d.VerificationId)
            .HasDatabaseName("idx_disputes_verification_id");
        
        builder.HasIndex(d => d.ComplainantId)
            .HasDatabaseName("idx_disputes_complainant_id");
        
        builder.HasIndex(d => d.RespondentId)
            .HasDatabaseName("idx_disputes_respondent_id");
        
        builder.HasIndex(d => d.AssignedTo)
            .HasDatabaseName("idx_disputes_assigned_to");
    }
}
