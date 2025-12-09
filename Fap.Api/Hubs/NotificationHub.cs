using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fap.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}

