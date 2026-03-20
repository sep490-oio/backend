using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions");

        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.WalletId)
            .HasColumnName("wallet_id")
            .IsRequired();

        builder.Property(wt => wt.TransactionId)
            .HasColumnName("transaction_id");

        builder.ComplexProperty(wt => wt.Type, typeBuilder =>
        {
            typeBuilder.Property(t => t.Id)
                .HasColumnName("type")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(wt => wt.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(wt => wt.BalanceBefore)
            .HasColumnName("balance_before")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(wt => wt.BalanceAfter)
            .HasColumnName("balance_after")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(wt => wt.Description)
            .HasColumnName("description");

        builder.Property(wt => wt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Indexes
        builder.HasIndex(wt => wt.WalletId)
            .HasDatabaseName("idx_wallet_transactions_wallet");
        builder.HasQueryFilter(wt => wt.Wallet.Type == WalletType.Platform || wt.Wallet.User!.DeletedAt == null);
    }
}
