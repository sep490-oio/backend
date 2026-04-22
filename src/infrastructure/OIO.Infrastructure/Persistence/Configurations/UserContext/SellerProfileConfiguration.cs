using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("seller_profiles");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StoreName)
            .HasColumnName("store_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.StoreDescription)
            .HasColumnName("store_description")
            .HasColumnType("text")
            .HasDefaultValue("There are no description for this store.")
            .IsRequired();

        builder.ComplexProperty(s => s.Status, statusBuilder =>
        {
            statusBuilder.Property(x => x.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired()
                .HasComplexIndex(indexName: "idx_seller_profiles_status");
        });

        builder.Property(s => s.VerifiedAt)
            .HasColumnName("verified_at");

        builder.Property(s => s.TotalSalesCount)
            .HasColumnName("total_sales_count")
            .HasDefaultValue(0);

        builder.Property(s => s.TotalSalesAmount)
            .HasColumnName("total_sales_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(s => s.TrustScoreOverall)
            .HasColumnName("trust_score_overall")
            .HasDefaultValue(0m)
            .HasPrecision(5, 2);

        builder.Property(s => s.TrustScoreCalculatedAt)
            .HasColumnName("trust_score_calculated_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        // Match User global filter to avoid required-principal filter warning.
        builder.HasQueryFilter(s => s.User.DeletedAt == null);
    }
}
