using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users", tb =>
            {
                tb.HasCheckConstraint("UserValidPhoneCheck", "PhoneNumber Like '01[0125]%' and PhoneNumber Not Like '%[^0-9]%'");
            });

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(11);

            builder.Property(x => x.JoinedAt)
                .HasColumnType("datetime2");

            builder.HasIndex(x => x.PhoneNumber)
                .IsUnique();

            builder.HasMany(x => x.Proposals)
                .WithOne(p => p.Vendor)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.VendorSLAContracts)
                .WithOne(s => s.Vendor)
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.ClientSLAContracts)
                .WithOne(s => s.Client)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.UserProfile)
                .IsRequired();

            builder.OwnsOne(x => x.UserPreference, userPreference =>
            {
                userPreference.Property(p => p.Id).HasColumnName("UserPreference_Id");
                userPreference.Property(p => p.EmailNotification)
                    .HasColumnName("UserPreference_EmailNotification")
                    .HasDefaultValue(true);
                userPreference.Property(p => p.SystemNotification)
                    .HasColumnName("UserPreference_SystemNotification")
                    .HasDefaultValue(true);
            });
        }
    }
}
