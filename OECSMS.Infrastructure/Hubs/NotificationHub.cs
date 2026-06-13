using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace OECSMS.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        public static readonly ConcurrentDictionary<int, List<string>> UserConnections = new();

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.Request.Query["userId"];

            if (int.TryParse(userIdStr, out int userId))
            {
                var connections = UserConnections.GetOrAdd(userId, _ => new List<string>());
                lock (connections)
                {
                    if (!connections.Contains(Context.ConnectionId))
                    {
                        connections.Add(Context.ConnectionId);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext?.Request.Query["userId"];

            if (int.TryParse(userIdStr, out int userId))
            {
                if (UserConnections.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(Context.ConnectionId);
                    }
                    if (connections.Count == 0)
                    {
                        UserConnections.TryRemove(userId, out _);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
