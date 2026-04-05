using CorpServe.Shared.DTOs.NotificationDTOs;

namespace CorpServe.Services.Abstraction
{
    public interface IRealtimeNotifier
    {
        Task NotifyUserAsync(string userId, NotificationDTO notification);
    }
}
