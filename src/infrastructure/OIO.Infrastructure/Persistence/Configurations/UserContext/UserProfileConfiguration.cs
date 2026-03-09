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

        builder.ComplexProperty(x => x.FirstName, firstNameBuilder =>
        {
            firstNameBuilder.Property(p => p.Value)
                .HasColumnName("first_name")
                .HasMaxLength(App.Constraint.FirstName.MaxLength)
                .IsRequired();
        });

        builder.ComplexProperty(x => x.LastName, lastNameBuilder =>
        {
            lastNameBuilder.Property(p => p.Value)
                .HasColumnName("last_name")
                .HasMaxLength(App.Constraint.LastName.MaxLength)
                .IsRequired();
        });
        
        builder.ComplexProperty(x => x.DisplayName, displayNameBuilder =>
        {
            displayNameBuilder.Property(p => p.Value)
                .HasColumnName("display_name")
                .HasMaxLength(App.Constraint.DisplayName.MaxLength)
                .IsRequired();
        });
        
        builder.ComplexProperty(x => x.AvatarUrl, avatarUrlBuilder =>
        {
            avatarUrlBuilder.Property(p => p.Value)
                .HasColumnName("avatar_url")
                .IsRequired();
        });


        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth");

        
        builder.ComplexProperty(x => x.Gender, genderBuilder =>
        {
            genderBuilder.Property(p => p.Id)
                .HasColumnName("gender")
                .IsRequired();
        });

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