using CorpServe.Domain.Entities.SpecializedCategoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class VendorCategoryConfiguration : IEntityTypeConfiguration<VendorCategory>
    {
        public void Configure(EntityTypeBuilder<VendorCategory> builder)
        {
            builder.ToTable("VendorCategories");

            // Composite Primary Key
            builder.HasKey(vc => new { vc.VendorId, vc.CategoryId });

            builder.HasOne(vc => vc.Vendor)
                   .WithMany(u => u.VendorCategories)
                   .HasForeignKey(vc => vc.VendorId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(vc => vc.Category)
                   .WithMany(c => c.VendorCategories)
                   .HasForeignKey(vc => vc.CategoryId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(vc => vc.VendorId);
            builder.HasIndex(vc => vc.CategoryId);
        }
    }
}
