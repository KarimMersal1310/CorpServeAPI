using CorpServe.Domain.Entities.RequestModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
    {
        public void Configure(EntityTypeBuilder<RequestAttachment> builder)
        {
            builder.ToTable("RequestAttachments");

            builder.HasKey(ra => ra.Id);
            builder.Property(ra => ra.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'REQA-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestAttachmentSequence AS VARCHAR(3)), 3)");

            builder.Property(ra => ra.FileUrl)
                .IsRequired();

            builder.Property(ra => ra.RequestId)
                .IsRequired();
        }
    }
}
