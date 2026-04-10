using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.ChatDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IChatService
    {
        Task<Result<IEnumerable<ChatRoomDTO>>> GetUserChatRoomsAsync(string userId);
        Task<Result<IEnumerable<MessageDTO>>> GetChatRoomMessagesAsync(string userId, string chatRoomId, int pageIndex, int pageSize);
        Task<Result<MessageDTO>> SendMessageAsync(string userId, SendMessageDTO dto);
        Task<Result<bool>> MarkMessagesAsReadAsync(string userId, string chatRoomId);
        Task<Result<int>> GetUnreadChatCountAsync(string userId);
        Task<Result<string>> GetChatRoomIdByRequestAsync(string userId, string requestId);
        Task<Result<ChatRoomParticipantsDTO>> GetRoomParticipantsAsync(string chatRoomId);
    }
}
