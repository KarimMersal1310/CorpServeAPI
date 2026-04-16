using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("'UP-' + RIGHT('000' + CAST(NEXT VALUE FOR UserProfileSequence AS VARCHAR(3)), 3)");

            builder.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.ProfilePictureUrl)
                .HasMaxLength(200);

            builder.Property(x => x.CompanyLocation)
                .HasMaxLength(200);

            builder.Property(x => x.CompanyName)
                .HasMaxLength(200);

            builder.Property(x => x.VendorStars)
                .HasColumnType("decimal(3,1)");

            builder.HasOne(x => x.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.User)
                .IsRequired();

            builder.HasMany(x => x.Documents)
                .WithOne(d => d.Profile)
                .HasForeignKey(d => d.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
