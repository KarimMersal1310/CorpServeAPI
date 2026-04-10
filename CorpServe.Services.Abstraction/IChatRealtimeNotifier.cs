using CorpServe.Shared.DTOs.ChatDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IChatRealtimeNotifier
    {
        Task SendMessageToRoomAsync(string chatRoomId, MessageDTO message, string clientUserId, string vendorUserId);
        Task NotifyMessagesReadAsync(string chatRoomId, string readByUserId, string otherUserId);
    }
}
