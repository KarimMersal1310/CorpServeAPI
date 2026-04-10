using CorpServe.Domain.Entities.ChatModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpServe.Presistence.Data.Configurations
{
    public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
    {
        public void Configure(EntityTypeBuilder<ChatRoom> builder)
        {
            builder.ToTable("ChatRooms");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .HasMaxLength(12)
                .HasDefaultValueSql("'CHR-' + RIGHT('000' + CAST(NEXT VALUE FOR ChatRoomSequence AS VARCHAR(3)), 3)");

            builder.Property(c => c.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2");

            builder.Property(c => c.Status)
                .IsRequired();

            builder.Property(c => c.ClientId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(c => c.VendorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasOne(c => c.Client)
                .WithMany(u => u.ClientChatRooms)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Vendor)
                .WithMany(u => u.VendorChatRooms)
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.ChatRoom)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
