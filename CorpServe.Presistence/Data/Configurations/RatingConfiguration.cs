using CorpServe.Domain.Entities.RatingModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasMaxLength(12)
                .HasDefaultValueSql("'RAT-' + RIGHT('000' + CAST(NEXT VALUE FOR RatingSequence AS VARCHAR(3)), 3)");

            builder.Property(r => r.Stars)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.Property(r => r.CreatedAt)
                .HasColumnType("datetime2");

            builder.Property(r => r.IsLocked)
                .HasDefaultValue(true);

            builder.HasOne(r => r.Request)
                .WithOne(req => req.Rating)
                .HasForeignKey<Rating>(r => r.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Payment)
                .WithOne(p => p.Rating)
                .HasForeignKey<Rating>(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Client)
                .WithMany(u => u.ClientRatings)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Vendor)
                .WithMany(u => u.VendorRatings)
                .HasForeignKey(r => r.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.RequestId).IsUnique();
            builder.HasIndex(r => r.PaymentId).IsUnique();
        }
    }
}
