using CorpServe.Domain.Entities.SpecializedCategoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);
            builder.Property(v => v.Id)
                   .HasMaxLength(10)
                   .HasDefaultValueSql(
                       "'C-' + RIGHT('000' + CAST(NEXT VALUE FOR CategorySequence AS VARCHAR(3)), 3)"
                   );

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(500);
        }
    }
}
