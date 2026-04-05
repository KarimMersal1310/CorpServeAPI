using CorpServe.Domain.Entities.AIEstimateModule;
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
        private const int MaxRequestDescriptionLength = 500;

        public void Configure(EntityTypeBuilder<Request> builder)
        {
            builder.ToTable("Requests", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Requests_Discription_MaxLength",
                    $"LEN([Discription]) <= {MaxRequestDescriptionLength}");
            });

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .HasMaxLength(10)
                .HasDefaultValueSql("'REQ-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestSequence AS VARCHAR(3)), 3)");

            builder.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(r => r.Discription)
                .IsRequired()
                .HasMaxLength(MaxRequestDescriptionLength);

            builder.Property(r => r.BudgetMin)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(r => r.BudgetMax)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            builder.Property(r => r.ExpectedDeadline)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(p => p.CreatedAt)
               .IsRequired()
               .HasColumnType("datetime2");

            builder.HasMany(R => R.RequestAttachments)
                   .WithOne(RA => RA.Request)
                   .HasForeignKey(RA => RA.RequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(R => R.Category)
                   .WithMany(C => C.Requests)
                   .HasForeignKey(R => R.CateogryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(R => R.AIEstimation)
                   .WithOne(AI => AI.Request)
                   .HasForeignKey<AIEstimation>(AI => AI.RequestId)
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

                RequestProgress.Property(p => p.VendorId)
                               .HasColumnName("RequestProgress_VendorId")
                               .HasMaxLength(450)
                               .IsRequired(false);

                RequestProgress.Property(p => p.Percentage)
                               .IsRequired();

                RequestProgress.Property(p => p.Description)
                               .IsRequired()
                               .HasMaxLength(500);

                RequestProgress.Property(p => p.UpdatedAt)
                               .IsRequired()
                               .HasColumnType("datetime2");

                RequestProgress.HasOne(p => p.Vendor)
                               .WithMany()
                               .HasForeignKey(p => p.VendorId)
                               .OnDelete(DeleteBehavior.Restrict);
    
            });
        }
    }
}
