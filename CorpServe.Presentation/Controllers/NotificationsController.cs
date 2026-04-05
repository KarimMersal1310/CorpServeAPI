using CorpServe.Services.Abstraction;
using CorpServe.Shared;
using CorpServe.Shared.DTOs.NotificationDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    [Authorize]
    public class NotificationsController : ApiBaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("my")]
        public async Task<ActionResult<PaginatedResult<NotificationDTO>>> GetMyNotifications([FromQuery] NotificationQueryParams queryParams)
        {
            var result = await _notificationService.GetUserNotificationsAsync(GetUserIdFromToken(), queryParams);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var result = await _notificationService.GetUnreadCountAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpPost("{notificationId}/read")]
        public async Task<ActionResult<bool>> MarkAsRead(string notificationId)
        {
            var result = await _notificationService.MarkAsReadAsync(GetUserIdFromToken(), notificationId);
            return HandleResult(result);
        }

        [HttpPost("read-all")]
        public async Task<ActionResult<bool>> MarkAllAsRead()
        {
            var result = await _notificationService.MarkAllAsReadAsync(GetUserIdFromToken());
            return HandleResult(result);
        }
    }
}
