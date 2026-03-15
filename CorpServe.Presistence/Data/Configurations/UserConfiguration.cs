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

            builder.HasIndex(x => x.PhoneNumber)
                .IsUnique();

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
