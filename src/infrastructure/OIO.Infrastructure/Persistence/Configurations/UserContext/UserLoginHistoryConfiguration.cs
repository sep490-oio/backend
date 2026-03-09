using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserLoginHistoryConfiguration : IEntityTypeConfiguration<UserLoginHistory>
{
    public void Configure(EntityTypeBuilder<UserLoginHistory> builder)
    {
        builder.ToTable("user_login_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserLoginHistoryId.From(value));

        builder.Property(h => h.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(h => h.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet")
            .IsRequired();

        builder.Property(h => h.UserAgent)
            .HasColumnName("user_agent")
            .IsRequired();

        builder.Property(h => h.LoginAt)
            .HasColumnName("login_at")
            .IsRequired();

        builder.ComplexProperty(x => x.Status, statusBuilder =>
        {
            statusBuilder.Property(h => h.Id)
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();
        });
    }
}