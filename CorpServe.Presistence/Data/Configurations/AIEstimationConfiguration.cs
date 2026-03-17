using CorpServe.Domain.Entities.AIEstimateModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class AIEstimationConfiguration : IEntityTypeConfiguration<AIEstimation>
    {
        public void Configure(EntityTypeBuilder<AIEstimation> builder)
        {
            builder.ToTable("AIEstimation");

            builder.HasKey(ai => ai.Id);

            builder.Property(ai => ai.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'AIE-' + RIGHT('000' + CAST(NEXT VALUE FOR AIEstimationSequence AS VARCHAR(3)), 3)");

            builder.Property(ai => ai.EstimatedCost)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(ai => ai.EstimatedTime)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(ai => ai.Confidence)
                .IsRequired();

            builder.Property(ai => ai.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(ai => ai.RequestId)
                .HasMaxLength(10)
                .IsRequired();
        }
    }
}
