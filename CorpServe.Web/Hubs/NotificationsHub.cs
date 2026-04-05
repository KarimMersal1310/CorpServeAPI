using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CorpServe.Web.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}
