using CorpServe.Domain.Entities.RequestModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Presistence.Data.Configurations
{
    public class RequestConfiguration : IEntityTypeConfiguration<Request>
    {
        public void Configure(EntityTypeBuilder<Request> builder)
        {
            builder.ToTable("Requests");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'REQ-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestSequence AS VARCHAR(3)), 3)");

            builder.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(r => r.Discription)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(r => r.BudgetMin)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(r => r.BudgetMax)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(r => r.ExpectedDeadline)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.HasMany(R => R.RequestAttachments)
                   .WithOne(RA => RA.Request)
                   .HasForeignKey(RA => RA.RequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(R => R.RequestProgress, RequestProgress =>
            {
                RequestProgress.Property(p => p.Id)
                               .HasColumnName("RequestProgress_Id")
                               .HasMaxLength(10)
                               .HasDefaultValueSql("'REQP-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestProgressSequence AS VARCHAR(3)), 3)");

                RequestProgress.Property(p => p.Percentage).HasColumnName("RequestProgress_ProgressPercentage");

                RequestProgress.Property(p => p.Description).HasColumnName("RequestProgress_Description");

                RequestProgress.Property(p => p.UpdatedAt).HasColumnName("RequestProgress_UpdatedAt");

                RequestProgress.Property(p => p.Percentage)
                               .IsRequired();

                RequestProgress.Property(p => p.Description)
                               .IsRequired()
                               .HasMaxLength(500);

                RequestProgress.Property(p => p.UpdatedAt)
                               .IsRequired()
                               .HasColumnType("datetime2");
    
            });
        }
    }
}
