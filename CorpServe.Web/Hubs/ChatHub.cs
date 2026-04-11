using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.ChatModule;
using CorpServe.Services.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Security.Claims;

namespace CorpServe.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public static string UserGroup(string userId) => $"user_{userId}";

        private readonly IServiceScopeFactory _scopeFactory;

        public ChatHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(string chatRoomId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(chatRoomId))
                return;

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repo = unitOfWork.GetRepository<ChatRoom, string>();
            var room = await repo.GetByIdAsync(chatRoomId);

            if (room is not null && room.ClientId != userId && room.VendorId != userId)
                return;

            if (room is null)
            {
                var matches = await repo.GetAllAsync(new ChatRoomForUserByIdLooseSpecification(userId, chatRoomId));
                room = matches.FirstOrDefault();
            }

            if (room is null || (room.ClientId != userId && room.VendorId != userId))
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id);
        }

        public async Task LeaveRoom(string chatRoomId)
        {
            if (!string.IsNullOrWhiteSpace(chatRoomId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId);
        }
    }
}
