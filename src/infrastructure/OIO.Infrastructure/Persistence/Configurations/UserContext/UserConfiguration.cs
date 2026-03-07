using System.Net;
using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.AppDefinitions;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        // ==================== Primary Key ====================
        builder.HasKey(u => u.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserId.From(value));

        // ==================== Properties ====================
        builder.ComplexProperty(e => e.UserName, userNameBuilder =>
            {
                userNameBuilder.Property(userName => userName.Value)
                    .HasMaxLength(App.Constraint.UserName.MaxLength)
                    .HasColumnName("user_name");
                
                userNameBuilder.Property(normalizedUserName => normalizedUserName.Normalized)
                    .HasMaxLength(App.Constraint.UserName.MaxLength)
                    .HasComputedColumnSql("upper((user_name)::text)", true)
                    .HasColumnName("normalized_user_name")
                    .HasComplexIndex(
                        isUnique: true,
                        filter: "(deleted_at IS NULL)",
                        indexName: "idx_unique_users_normalized_user_name_active");
                
            });
        
        builder.ComplexProperty(e => e.Email, emailBuilder =>
        {
            emailBuilder.Property(email => email.Value)
                .HasMaxLength(App.Constraint.UserEmail.MaxLength)
                .HasColumnName("email")
                .IsRequired();
                
            emailBuilder.Property(normalizedEmail => normalizedEmail.Normalized)
                .HasMaxLength(App.Constraint.UserEmail.MaxLength)
                .HasComputedColumnSql("upper((email)::text)", true)
                .HasColumnName("normalized_email")
                .HasComplexIndex(
                    isUnique: true,
                    filter: "(deleted_at IS NULL)",
                    indexName: "idx_unique_users_normalized_email_active");
        });

        builder.Property(u => u.EmailConfirmed)
            .HasColumnName("email_confirmed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.EmailConfirmedAt)
            .HasColumnName("email_confirmed_at");

        builder.Property(u => u.Password)
            .HasColumnName("password_hash")
            .HasConversion(password => password!.HashedValue, value => Password.CreateFromHash(value));

        builder.ComplexProperty(u => u.PhoneNumber, phoneNumberBuilder =>
            {
                phoneNumberBuilder.Property(u => u.Value)
                    .HasColumnName("phone_number")
                    .HasMaxLength(20);

                phoneNumberBuilder.Property(u => u.CountryCode)
                    .HasColumnName("phone_number_country_code")
                    .HasMaxLength(10);
            });
        

        builder.Property(u => u.PhoneNumberConfirmed)
            .HasColumnName("phone_number_confirmed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.PhoneNumberConfirmedAt)
            .HasColumnName("phone_number_confirmed_at");

        builder.Property(u => u.TwoFactorEnabled)
            .HasColumnName("two_factor_enabled")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.TwoFactorProvider)
            .HasColumnName("two_factor_provider")
            .HasMaxLength(30)
            .HasDefaultValue(TwoFactorProvider.None)
            .IsRequired()
            .HasConversion(provider => provider.Id, value => TwoFactorProvider.FromId(value).GetValueOrThrow());

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue(UserStatus.Inactive)
            .IsRequired()
            .HasConversion(staus => staus.Id, value => UserStatus.FromId(value).GetValueOrThrow());

        builder.Property(u => u.LockoutEnabled)
            .HasColumnName("lockout_enabled")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(u => u.AccessFailedCount)
            .HasColumnName("access_failed_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(u => u.Version)
            .HasColumnName("version")
            .HasDefaultValue(0)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(u => u.LockoutReason)
            .HasColumnName("lockout_reason");

        // ==================== Relationships ====================
        builder.HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Addresses)
            .WithOne()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Permissions)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.LoginHistories)
            .WithOne()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Sessions)
            .WithOne()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Query Filters ====================
        builder.HasQueryFilter(u => u.DeletedAt == null);

        // ==================== Ignore ====================
        builder.Ignore(u => u.DomainEvents);
        builder.Ignore(u => u.IsDeleted);
    }
}