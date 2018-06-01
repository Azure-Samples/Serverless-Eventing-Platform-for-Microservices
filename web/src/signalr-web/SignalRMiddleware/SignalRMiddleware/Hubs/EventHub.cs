using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRMiddleware.Hubs
{
    public class EventHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            string userId = httpContext.Request.Query["userId"];
            
            Connections._connections.Add(userId, Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            string userId = httpContext.Request.Query["userId"];

            Connections._connections.Remove(userId, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

    }
}
