using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

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