using CorpServe.Domain.Entities.ProposalModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class SLAContractConfiguration : IEntityTypeConfiguration<SLAContract>
    {
        public void Configure(EntityTypeBuilder<SLAContract> builder)
        {
            builder.ToTable("SLAContracts");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'SLA-' + RIGHT('000' + CAST(NEXT VALUE FOR SLAContractSequence AS VARCHAR(3)), 3)");

            builder.Property(s => s.ContractPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(s => s.Deadline)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(s => s.SLAStatus)
                .IsRequired();

            builder.Property(s => s.VendorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(s => s.ClientId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(s => s.RequestId)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(s => s.ProposalId)
                .HasMaxLength(10)
                .IsRequired();

            builder.HasOne(s => s.Vendor)
                .WithMany(u => u.VendorSLAContracts)
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Client)
                .WithMany(u => u.ClientSLAContracts)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Request)
                .WithOne(r => r.SLAContract)
                .HasForeignKey<SLAContract>(s => s.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Proposal)
                .WithOne(p => p.SLAContract)
                .HasForeignKey<SLAContract>(s => s.ProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.VendorId, s.SLAStatus });
            builder.HasIndex(s => s.RequestId).IsUnique();
        }
    }
}
