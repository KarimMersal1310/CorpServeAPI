using CorpServe.Domain.Entities.VendorVerifyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Presistence.Data.Configurations
{
    public class VendorCertificateConfiguration : IEntityTypeConfiguration<VendorCertificate>
    {
        public void Configure(EntityTypeBuilder<VendorCertificate> builder)
        {
            builder.ToTable("VendorCertificates");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FileUrl).IsRequired();
            builder.Property(c => c.CertificateType).HasMaxLength(100).IsRequired();

            // Relationship: VendorCertificate -> VendorVerify
            builder.HasOne(c => c.VendorVerify)
                   .WithMany(v => v.VendorCertificates)
                   .HasForeignKey(c => c.VendorVerifyId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
