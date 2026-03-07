using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

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