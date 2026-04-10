using CorpServe.Domain.Entities.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.ChatModule
{
    public class ChatRoom : BaseEntity<string>
    {
        public DateTime CreatedAt { get; set; }
        public ChatRoomStatus Status { get; set; } = ChatRoomStatus.Active;

        #region RelationShips

        #region ChatRoom - Message (1-M)

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        #endregion

        #region ChatRoom - Client (M-1)
        public string ClientId { get; set; } = default!;
        public ApplicationUser Client { get; set; } = default!;

        #endregion

        #region ChatRoom - Vendor (M-1)
        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;

        #endregion

        #endregion

    }
}
