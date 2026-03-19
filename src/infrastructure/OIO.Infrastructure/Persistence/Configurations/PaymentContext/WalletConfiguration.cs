using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired(false);

        builder.Property(w => w.Type)
            .HasColumnName("wallet_type")
            .HasMaxLength(20)
            .HasDefaultValue(WalletType.Personal)
            .HasConversion(x => x.Id, x => WalletType.FromId(x).Value)
            .IsRequired();

        builder.ComplexProperty(w => w.WalletFunds, walletFunds =>
        {
            walletFunds.Property(cp => cp.BalanceAmount)
                .HasColumnName("balance")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            walletFunds.Property(cp => cp.PendingBalanceAmount)
                .HasColumnName("pending_balance")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            walletFunds.ComplexProperty(p => p.Currency, currency =>
            {
                currency.Property(cp => cp.Id)
                    .HasColumnName("currency")
                    .IsRequired();

                currency.Ignore(cp => cp.Symbol);
            });

            walletFunds.Ignore(cp => cp.PendingBalance);
            walletFunds.Ignore(cp => cp.Balance);
        });

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(w => w.Version)
            .HasColumnName("version")
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(w => w.ModifiedAt)
            .HasColumnName("modified_at");

        // Navigation
        builder.HasMany(w => w.WalletTransactions)
            .WithOne(wt => wt.Wallet)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Constraints
        // Unique index on user_id only for personal wallets (platform wallet has no user)
        builder.HasIndex(w => w.UserId)
            .IsUnique()
            .HasDatabaseName("uq_wallets_user_id")
            .HasFilter("wallet_type = 'personal'");

        // Only one platform wallet allowed
        builder.HasIndex(w => w.Type)
            .IsUnique()
            .HasDatabaseName("uq_wallets_platform")
            .HasFilter("wallet_type = 'platform'");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_non_negative_balance", "balance >= 0");
            t.HasCheckConstraint("chk_non_negative_pending", "pending_balance >= 0");
        });

        // Platform wallet has no user, so allow null UserId in query filter
        builder.HasQueryFilter(w => w.Type == WalletType.Platform || w.User!.DeletedAt == null);
    }
}
