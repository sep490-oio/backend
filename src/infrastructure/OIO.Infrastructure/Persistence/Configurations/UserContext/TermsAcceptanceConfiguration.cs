using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class TermsAcceptanceConfiguration : IEntityTypeConfiguration<TermsAcceptance>
{
    public void Configure(EntityTypeBuilder<TermsAcceptance> builder)
    {
        builder.ToTable("user_terms_acceptances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.TermDocumentId)
            .HasColumnName("term_document_id")
            .IsRequired();

        builder.Property(a => a.AcceptedAt)
            .HasColumnName("accepted_at")
            .IsRequired();

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet");

        builder.Property(a => a.UserAgent)
            .HasColumnName("user_agent");

        // Navigation
        builder.HasOne(a => a.TermDocument)
            .WithMany()
            .HasForeignKey(a => a.TermDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Constraints
        builder.HasIndex(a => new { a.UserId, a.TermDocumentId })
            .IsUnique()
            .HasDatabaseName("uq_user_terms_acceptances_user_term");

        // Indexes
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("idx_user_terms_acceptances_user");

        builder.HasIndex(a => a.TermDocumentId)
            .HasDatabaseName("idx_user_terms_acceptances_term_document");

        builder.HasIndex(a => a.AcceptedAt)
            .HasDatabaseName("idx_user_terms_acceptances_accepted_at");
    }
}
