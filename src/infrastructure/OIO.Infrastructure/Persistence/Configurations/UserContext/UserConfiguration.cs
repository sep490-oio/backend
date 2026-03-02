using System.Net;
using EFCore.ComplexIndexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Outbox;
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

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(App.Constraint.FirstName.MaxLength)
            .HasConversion(x => x!.Value, value => FirstName.Create(value).GetValueOrDefault());

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(App.Constraint.LastName.MaxLength)
            .HasConversion(x => x!.Value, value => LastName.Create(value).GetValueOrDefault());

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(App.Constraint.DisplayName.MaxLength)
            .HasConversion(x => x!.Value, value => DisplayName.Create(value).GetValueOrDefault());

        builder.Property(p => p.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasConversion(x => x!.Value, value => AvatarUrl.Create(value).GetValueOrDefault());

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.Property(p => p.Gender)
            .HasColumnName("gender")
            .HasMaxLength(10)
            .HasConversion(x => x!.Id, value => Gender.FromId(value).GetValueOrThrow());

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Ignore ====================
        builder.Ignore(p => p.FullName);
    }
}

internal sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("user_addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserAddressId.From(value));

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(a => a.Type)
            .HasColumnName("type")
            .HasMaxLength(10)
            .HasDefaultValue(AddressType.Other)
            .IsRequired()
            .HasConversion(x => x.Id, value => AddressType.FromId(value).GetValueOrThrow());

        builder.Property(a => a.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.ComplexProperty(u => u.PhoneNumber, phoneNumberBuilder =>
        {
            phoneNumberBuilder.Property(u => u.Value)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            phoneNumberBuilder.Property(u => u.CountryCode)
                .HasColumnName("phone_number_country_code")
                .HasMaxLength(10)
                .IsRequired();
        });

        // ==================== Address Value Object ====================
        builder.ComplexProperty(a => a.Address, addressBuilder =>
        {
            addressBuilder.Property(ad => ad.Street)
                .HasColumnName("address")
                .HasMaxLength(App.Constraint.Address.StreetMaxLength)
                .IsRequired();

            addressBuilder.Property(ad => ad.Ward)
                .HasColumnName("ward")
                .HasMaxLength(App.Constraint.Address.WardMaxLength)
                .IsRequired();

            addressBuilder.Property(ad => ad.District)
                .HasColumnName("district")
                .HasMaxLength(App.Constraint.Address.DistrictMaxLength)
                .IsRequired();

            addressBuilder.Property(ad => ad.City)
                .HasColumnName("city")
                .HasMaxLength(App.Constraint.Address.CityMaxLength)
                .IsRequired();

            addressBuilder.Property(ad => ad.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(App.Constraint.Address.PostalCodeMaxLenght);
        });

        builder.Property(a => a.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(a => a.ModifiedAt)
            .HasColumnName("modified_at");

        // ==================== Indexes ====================
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("idx_user_addresses_user_id");

        builder.HasIndex(a => new { a.UserId, a.Type })
            .HasDatabaseName("idx_user_addresses_user_id_type");

        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasDatabaseName("idx_unique_default_address_per_user")
            .HasFilter("is_default = TRUE");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        // Composite primary key
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Property(ur => ur.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => RoleId.From(value));

        builder.Property(ur => ur.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ==================== Indexes ====================
        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("ix_user_roles_role_id");
    }
}

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");

        // Composite primary key
        builder.HasKey(up => new { up.UserId, up.PermissionId });

        builder.Property(up => up.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(up => up.PermissionId)
            .HasColumnName("permission_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => PermissionId.From(value));

        builder.Property(up => up.IsAllowed)
            .HasColumnName("is_allowed")
            .HasDefaultValue(true)
            .IsRequired();
    }
}

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

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(x => x.Id, value => LoginStatus.FromId(value).Value);
    }
}

internal sealed class RefreshTokenFamilyConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserSessionId.From(value));

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(f => f.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        builder.Property(f => f.UserAgent)
            .HasColumnName("user_agent")
            .IsRequired();

        builder.Property(f => f.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("inet")
            .IsRequired();

        builder.Property(f => f.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(f => f.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
        
        builder.Property(f => f.AbsoluteExpiresAt)
            .HasColumnName("absolute_expires_at")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(f => f.LastRotatedAt)
            .HasColumnName("last_rotated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(f => f.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(f => f.RevokedReason)
            .HasColumnName("revoked_reason");

        // ==================== Relationships ====================
        builder.HasMany(f => f.Tokens)
            .WithOne(t => t.RefreshTokenFamily)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================== Indexes ====================
        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("ix_user_sessions_user");

        builder.HasIndex(f => new { f.UserId, f.IsActive })
            .HasDatabaseName("ix_user_sessions_user_active");
        
        builder.HasIndex(f => f.ExpiresAt)
            .HasDatabaseName("ix_user_sessions_expires_at");
        
        builder.HasIndex(f => f.AbsoluteExpiresAt)
            .HasDatabaseName("ix_user_sessions_absolute_expires_at");
        
        
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens", t =>
        {
            t.HasCheckConstraint("chk_expires_after_created",
                "expires_at > created_at");

            t.HasCheckConstraint("chk_revoked_after_created",
                "revoked_at IS NULL OR revoked_at >= created_at");

            t.HasCheckConstraint("chk_used_after_created",
                "used_at IS NULL OR used_at >= created_at");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => UserRefreshTokenId.From(value));

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserId.From(value));

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .IsRequired();

        builder.Property(t => t.SessionId)
            .HasColumnName("session_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => UserSessionId.From(value));

        builder.Property(t => t.ParentTokenId)
            .HasColumnName("parent_token_id")
            .HasConversion(x => x!.Value.Value, value => UserRefreshTokenId.From(value));

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(t => t.RevokedReason)
            .HasColumnName("revoked_reason");

        builder.Property(t => t.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasColumnType("inet")
            .IsRequired();

        builder.Property(t => t.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasColumnType("inet");

        builder.Property(t => t.RotationCounter)
            .HasColumnName("rotation_counter")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(t => t.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.UsedAt)
            .HasColumnName("used_at");

        // ==================== Self-referencing relationship ====================
        builder.HasOne<UserRefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ParentTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        // ==================== Indexes ====================
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_user_refresh_tokens_token_hash");

        builder.HasIndex(t => t.SessionId)
            .HasDatabaseName("ix_user_refresh_session_id");

        builder.HasIndex(t => new { SessionId = t.SessionId, t.CreatedAt })
            .HasDatabaseName("ix_user_refresh_tokens_session_session_id_created_at");

        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("ix_user_refresh_tokens_expires_at");

        builder.HasIndex(t => new { t.UserId, t.IsUsed })
            .HasDatabaseName("ix_user_refresh_tokens_user_id_is_used");

        // ==================== Ignore computed properties ====================
        builder.Ignore(t => t.IsRevoked);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => RoleId.From(value));

        builder.Property(r => r.RoleName)
            .HasColumnName("role_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.NormalizedRoleName)
            .HasColumnName("normalized_role_name")
            .HasMaxLength(150)
            .HasComputedColumnSql("UPPER(role_name)", stored: true);

        builder.HasIndex(r => r.NormalizedRoleName)
            .IsUnique();

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Ignore(r => r.DomainEvents);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id")
            .HasConversion(x => x.Value, value => PermissionId.From(value));

        builder.Property(p => p.PermissionCode)
            .HasColumnName("permission_code")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.NormalizedPermissionCode)
            .HasColumnName("normalized_permission_code")
            .HasMaxLength(150)
            .HasComputedColumnSql("UPPER(permission_code)", stored: true);

        builder.HasIndex(p => p.NormalizedPermissionCode)
            .IsUnique();
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Property(rp => rp.RoleId)
            .HasColumnName("role_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => RoleId.From(value));

        builder.Property(rp => rp.PermissionId)
            .HasColumnName("permission_id")
            .IsRequired()
            .HasConversion(x => x.Value, value => PermissionId.From(value));

        builder.Property(rp => rp.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OutboxMessageTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(outboxMessage => outboxMessage.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(outboxMessage => outboxMessage.Type)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("type");

        builder.Property(outboxMessage => outboxMessage.Content)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasColumnName("content");

        builder
            .Property(outboxMessage => outboxMessage.OccurredAt)
            .IsRequired()
            .HasColumnName("occurred_at");

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(o => o.Error)
            .HasColumnName("error");

        builder.Property(o => o.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("attempt_count");

        builder
            .HasIndex(o => new { o.OccurredAt })
            .HasDatabaseName("idx_outbox_messages_unprocessed")
            .HasFilter("processed_at IS NULL");

        builder
            .HasIndex(om => new { om.OccurredAt, om.Id })
            .HasDatabaseName("idx_outbox_cleanup")
            .HasFilter("processed_at IS NOT NULL AND error IS NULL");
    }
}