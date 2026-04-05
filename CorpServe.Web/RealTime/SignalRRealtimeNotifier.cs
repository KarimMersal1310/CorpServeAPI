using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.NotificationDTOs;
using CorpServe.Shared.Notifications;
using CorpServe.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CorpServe.Web.RealTime
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<NotificationsHub> _hubContext;

        public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyUserAsync(string userId, NotificationDTO notification)
        {
            await _hubContext.Clients.User(userId).SendAsync(NotificationEventKeys.NotificationReceived, notification);
        }
    }
}
