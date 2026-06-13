using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using OECSMS.Application.Interfaces;
using OECSMS.Infrastructure.Hubs;
using Task = System.Threading.Tasks.Task;

namespace OECSMS.Infrastructure.Services
{
    public class NotificationHubContext : INotificationHubContext
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubContext(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(int userId, string title, string message, string type, int? relatedEntityId)
        {
            if (NotificationHub.UserConnections.TryGetValue(userId, out var connections))
            {
                List<string> connList;
                lock (connections)
                {
                    connList = new List<string>(connections);
                }

                foreach (var connectionId in connList)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                    {
                        title,
                        message,
                        type,
                        relatedEntityId,
                        createdAt = DateTime.UtcNow
                    });
                }
            }
        }

        public async Task SendQueueUpdateAsync()
        {
            // Broadcast to everyone (since customers, assistants, and managers are interested in queue updates)
            await _hubContext.Clients.All.SendAsync("QueueUpdated");
        }

        public async Task SendTaskUpdateToManagerAsync(int managerId, int taskId, string status)
        {
            await SendNotificationToUserAsync(managerId, "Task Status Updated", $"Task ID {taskId} is now {status}.", "TaskUpdate", taskId);
        }
    }
}
