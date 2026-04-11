using CorpServe.Domain.Entities;
using CorpServe.Domain.Entities.ChatModule;

namespace CorpServe.Services.Specifications
{
    public class ChatRoomsByUserSpecification : BaseSpecificactions<ChatRoom, string>
    {
        public ChatRoomsByUserSpecification(string userId)
            : base(r => r.ClientId == userId || r.VendorId == userId)
        {
            AddInclude(r => r.Client);
            AddInclude(r => r.Vendor);
            AddInclude(r => r.Messages);
        }
    }

    public class ChatRoomByIdWithUsersSpecification : BaseSpecificactions<ChatRoom, string>
    {
        public ChatRoomByIdWithUsersSpecification(string chatRoomId)
            : base(r => r.Id == chatRoomId)
        {
            AddInclude(r => r.Client);
            AddInclude(r => r.Vendor);
        }
    }

    public class ChatRoomByClientAndVendorSpecification : BaseSpecificactions<ChatRoom, string>
    {
        public ChatRoomByClientAndVendorSpecification(string clientId, string vendorId)
            : base(r => r.ClientId == clientId && r.VendorId == vendorId)
        {
        }
    }

    /// <summary>Resolves a chat room for a user when route/id casing may not match the stored PK.</summary>
    public class ChatRoomForUserByIdLooseSpecification : BaseSpecificactions<ChatRoom, string>
    {
        public ChatRoomForUserByIdLooseSpecification(string userId, string chatRoomId)
            : base(r =>
                (r.ClientId == userId || r.VendorId == userId) &&
                r.Id.ToLower() == chatRoomId.ToLower())
        {
        }
    }

    public class MessagesByChatRoomSpecification : BaseSpecificactions<Message, string>
    {
        public MessagesByChatRoomSpecification(string chatRoomId, int pageIndex, int pageSize)
            : base(m => m.ChatRoomId == chatRoomId && m.DeletedAt == null)
        {
            AddOrderByDescending(m => m.SentAt);
            ApplyPagination(pageSize, pageIndex);
        }
    }
}
