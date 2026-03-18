using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.AuctionContext;

public class AuctionDepositConfiguration : IEntityTypeConfiguration<AuctionDeposit>
{
    public void Configure(EntityTypeBuilder<AuctionDeposit> builder)
    {
        builder.ToTable("auction_deposits");
        
        builder.HasIndex(e => new { e.AuctionId, UserId = e.BidderId }, "auction_deposits_auction_id_user_id_key")
            .IsUnique();
        
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => AuctionDepositId.From(x));
        
        builder.Property(a => a.BidderId)
            .HasColumnName("bidder_id")
            .ValueGeneratedNever()
            .HasConversion(x => x.Value, x => UserId.From(x));
        
        builder.Property(a => a.TransactionId)
            .HasColumnName("transaction_id")
            .ValueGeneratedNever()
            .HasConversion(x => x.HasValue ? x.Value.Value : default(Guid?), x => x.HasValue ? TransactionId.From(x.Value) : null);
        
        builder.ComplexProperty(a => a.Amount, amountBuilder =>
        {
           amountBuilder.Property(m => m.Amount) 
               .HasColumnName("amount")
               .HasPrecision(18, 2)
               .IsRequired();

           amountBuilder.ComplexProperty(m => m.Currency, currencyBuilder =>
           {
               currencyBuilder.Property(c => c.Id)
                   .HasColumnName("currency")
                   .HasMaxLength(3)
                   .IsRequired();

               currencyBuilder.Ignore(x => x.Symbol);
           });
        });
        
        builder.ComplexProperty(a => a.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });
        
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ReleasedAt)
            .HasColumnName("released_at");
        
        //Relationship
        builder.HasOne(d => d.Auction)
            .WithMany(a => a.Deposits)
            .HasForeignKey(d => d.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(d => d.Transaction)
            .WithMany(p => p.AuctionDeposits)
            .HasForeignKey(d => d.TransactionId)
            .HasConstraintName("auction_deposits_transaction_id_fkey");

        builder.HasOne(d => d.Bidder)
             .WithMany(p => p.AuctionDeposits)
             .HasForeignKey(d => d.BidderId)
             .OnDelete(DeleteBehavior.ClientSetNull)
             .HasConstraintName("auction_deposits_user_id_fkey");

        // Match User global filter to avoid required-principal filter warning.
        builder.HasQueryFilter(d => d.Bidder.DeletedAt == null);
    }
}