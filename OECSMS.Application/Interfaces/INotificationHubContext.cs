using System.Threading.Tasks;

namespace OECSMS.Application.Interfaces
{
    public interface INotificationHubContext
    {
        Task SendNotificationToUserAsync(int userId, string title, string message, string type, int? relatedEntityId);
        Task SendQueueUpdateAsync();
        Task SendTaskUpdateToManagerAsync(int managerId, int taskId, string status);
    }
}
