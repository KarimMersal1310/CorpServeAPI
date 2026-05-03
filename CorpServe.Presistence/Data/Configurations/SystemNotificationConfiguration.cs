using CorpServe.Domain.Entities.NotificationModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
    {
        public void Configure(EntityTypeBuilder<SystemNotification> builder)
        {
            builder.ToTable("SystemNotifications");

            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id)
                .HasMaxLength(12)
                .HasDefaultValueSql("'N-' + RIGHT('0000' + CAST(NEXT VALUE FOR NotificationSequence AS VARCHAR(4)), 4)");

            builder.Property(n => n.RecipientId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(n => n.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(n => n.Type)
                .IsRequired();

            builder.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(n => n.RelatedEntityId)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(n => n.RelatedEntityType)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.HasOne(n => n.Recipient)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
            builder.HasIndex(n => n.CreatedAt);
        }
    }
}
