using CorpServe.Domain.Entities.PaymentModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Presistence.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                   .HasMaxLength(12)
                   .HasDefaultValueSql("'PAY-' + RIGHT('000' + CAST(NEXT VALUE FOR PaymentSequence AS VARCHAR(3)), 3)");

            builder.HasOne(p => p.Request)
                   .WithOne(r => r.Payment)
                   .HasForeignKey<Payment>(p => p.RequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Client)
                   .WithMany(C => C.ClientPayments)
                   .HasForeignKey(p => p.ClientId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Vendor)
                   .WithMany(C => C.VendorPayments)
                   .HasForeignKey(p => p.VendorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.CreatedAt)
                   .HasColumnType("datetime2");

            builder.Property(p => p.Amount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Commision)
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.TotalAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.VendorNetAmount)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0m);

            builder.Property(p => p.PayoutStatus)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .HasDefaultValue(PayoutStatus.NotStarted);

            builder.Property(p => p.PayoutReference)
                   .HasMaxLength(120);

            builder.Property(p => p.PayoutCompletedAt)
                   .HasColumnType("datetime2");

            builder.Property(p => p.PayoutFailureReason)
                   .HasMaxLength(1000);

            builder.Property(p => p.MerchantOrderId)
                   .IsRequired()
                   .HasMaxLength(120);

            builder.Property(p => p.PaymobIntentionId)
                   .HasMaxLength(100);

            builder.Property(p => p.ClientSecret)
                   .HasMaxLength(250);

            builder.Property(p => p.PaymobTransactionId)
                   .HasMaxLength(100);

            builder.Property(p => p.CheckoutUrl)
                   .HasMaxLength(1000);

            builder.Property(p => p.WebhookRawStatus)
                   .HasMaxLength(100);

            builder.Property(p => p.FailureReason)
                   .HasMaxLength(1000);

            builder.Property(p => p.WebhookReceivedAt)
                   .HasColumnType("datetime2");

            builder.Property(p => p.PaidAt)
                   .HasColumnType("datetime2");

            builder.HasIndex(p => p.MerchantOrderId)
                   .IsUnique();
            builder.HasIndex(p => p.RequestId).IsUnique();
            builder.HasIndex(p => new { p.PaymentStatus, p.CreatedAt });


        }
    }
}
