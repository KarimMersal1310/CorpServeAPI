using CorpServe.Domain.Entities.ChatModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities
{
    public class Message : BaseEntity<string>
    {
        public string Content { get; set; } = default!;
        public MessageType Type { get; set; }
        public SenderType Sender { get; set; }
        public DateTime SentAt { get; set; } = default!;
        public bool IsRead { get; set; } = false;
        public DateTime? DeletedAt { get; set; } // Soft Delete
        public string? MediaUrl { get; set; }   // storage URL if Type = Image or File
        public string? MediaMimeType { get; set; } // "image/jpeg", "application/pdf", etc.
        public long? MediaSizeBytes { get; set; }

        #region RelationShip

        public string ChatRoomId { get; set; } = default!;
        public ChatRoom ChatRoom { get; set; } = default!;

        #endregion
    }
}
