using CorpServe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id)
                .HasMaxLength(12)
                .HasDefaultValueSql("'MSG-' + RIGHT('000' + CAST(NEXT VALUE FOR MessageSequence AS VARCHAR(3)), 3)");

            builder.Property(m => m.Content)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(m => m.Type)
                .IsRequired();

            builder.Property(m => m.Sender)
                .IsRequired();

            builder.Property(m => m.SentAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(m => m.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.DeletedAt)
                .HasColumnType("datetime2")
                .IsRequired(false);

            builder.Property(m => m.MediaUrl)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(m => m.MediaMimeType)
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(m => m.MediaSizeBytes)
                .IsRequired(false);

            builder.Property(m => m.ChatRoomId)
                .HasMaxLength(12)
                .IsRequired();

            builder.HasOne(m => m.ChatRoom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
