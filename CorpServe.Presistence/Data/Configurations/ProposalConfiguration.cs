using CorpServe.Domain.Entities.ProposalModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
    {
        public void Configure(EntityTypeBuilder<Proposal> builder)
        {
            builder.ToTable("Proposals");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'P-' + RIGHT('000' + CAST(NEXT VALUE FOR ProposalSequence AS VARCHAR(3)), 3)");

            builder.Property(p => p.ProposalStatus)
                .IsRequired();

            builder.Property(p => p.ProposalType)
                .IsRequired();

            builder.Property(p => p.Message)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(p => p.ProposedPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);

            builder.Property(p => p.ProposedDeadline)
                .HasColumnType("datetime2")
                .IsRequired(false);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(p => p.ClientResponseAt)
                .HasColumnType("datetime2")
                .IsRequired(false);

            builder.Property(p => p.IsSelected)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.VendorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(p => p.RequestId)
                .HasMaxLength(10)
                .IsRequired();

            builder.HasOne(p => p.Vendor)
                .WithMany(v => v.Proposals)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Request)
                .WithMany(r => r.Proposals)
                .HasForeignKey(p => p.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
