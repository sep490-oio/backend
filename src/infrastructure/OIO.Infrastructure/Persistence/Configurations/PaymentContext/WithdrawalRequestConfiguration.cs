using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;

namespace OIO.Infrastructure.Persistence.Configurations.PaymentContext;

internal sealed class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.ToTable("withdrawal_requests");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(w => w.WalletId)
            .HasColumnName("wallet_id")
            .IsRequired();

        builder.Property(w => w.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(w => w.Fee)
            .HasColumnName("fee")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(w => w.NetAmount)
            .HasColumnName("net_amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.ComplexProperty(w => w.BankAccount, ba =>
        {
            ba.Property(b => b.BankName)
                .HasColumnName("bank_name")
                .HasMaxLength(100);

            ba.Property(b => b.AccountNumber)
                .HasColumnName("bank_account_number")
                .HasMaxLength(50);

            ba.Property(b => b.AccountHolder)
                .HasColumnName("bank_account_holder")
                .HasMaxLength(200);
        });

        builder.ComplexProperty(w => w.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(w => w.ProcessedBy)
            .HasColumnName("processed_by");

        builder.Property(w => w.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(w => w.RejectionReason)
            .HasColumnName("rejection_reason");

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
