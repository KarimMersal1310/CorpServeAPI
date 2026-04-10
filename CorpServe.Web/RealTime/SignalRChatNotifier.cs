using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.ChatDTOs;
using CorpServe.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CorpServe.Web.RealTime
{
    public class SignalRChatNotifier : IChatRealtimeNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRChatNotifier(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMessageToRoomAsync(string chatRoomId, MessageDTO message, string clientUserId, string vendorUserId)
        {
            var roomGroup = _hubContext.Clients.Group(chatRoomId);
            var clientGroup = _hubContext.Clients.Group(ChatHub.UserGroup(clientUserId));
            var vendorGroup = _hubContext.Clients.Group(ChatHub.UserGroup(vendorUserId));

            await Task.WhenAll(
                roomGroup.SendAsync("ReceiveMessage", message),
                clientGroup.SendAsync("NewChatMessage", message),
                vendorGroup.SendAsync("NewChatMessage", message)
            );
        }

        public async Task NotifyMessagesReadAsync(string chatRoomId, string readByUserId, string otherUserId)
        {
            var roomGroup = _hubContext.Clients.Group(chatRoomId);
            var otherUserGroup = _hubContext.Clients.Group(ChatHub.UserGroup(otherUserId));

            await Task.WhenAll(
                roomGroup.SendAsync("MessagesRead", chatRoomId, readByUserId),
                otherUserGroup.SendAsync("ChatMessagesRead", chatRoomId, readByUserId)
            );
        }
    }
}
