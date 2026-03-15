using CorpServe.Domain.Entities.VendorVerifyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class VendorVerifyConfiguration : IEntityTypeConfiguration<VendorVerify>
    {
        public void Configure(EntityTypeBuilder<VendorVerify> builder)
        {
            builder.ToTable("VendorVerifications");

            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
               .HasMaxLength(10)
               .HasDefaultValueSql(
                   "'V-' + RIGHT('000' + CAST(NEXT VALUE FOR VendorSequence AS VARCHAR(3)), 3)"
               );

            builder.HasOne(v => v.Vendor)
                   .WithMany()
                   .HasForeignKey(v => v.VendorId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Restrict);
            builder.Property(v => v.OrganizationName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(v => v.ReviewedAt)
                .IsRequired(false);

            builder.Property(v => v.RejectReason)
                .HasMaxLength(500)
                .IsRequired(false);
        }
    }
}
