using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class ProfileDocumentConfiguration : IEntityTypeConfiguration<ProfileDocument>
    {
        public void Configure(EntityTypeBuilder<ProfileDocument> builder)
        {
            builder.ToTable("ProfileDocuments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PD-' + RIGHT('000' + CAST(NEXT VALUE FOR ProfileDocumentSequence AS VARCHAR(3)), 3)");

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.DocumentType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.DocumentUrl)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.ProfileId)
                .HasMaxLength(20)
                .IsRequired();
        }
    }
}
