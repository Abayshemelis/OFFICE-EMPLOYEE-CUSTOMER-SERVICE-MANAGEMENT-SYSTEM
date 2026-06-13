using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Application.Interfaces;

namespace OECSMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("notifications")]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(CurrentUserId);
            return ApiResponse(notifications, "Notifications retrieved successfully.");
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            return ApiResponse<object?>(null, "Notification marked as read.");
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return ApiResponse<object?>(null, "All notifications marked as read.");
        }
    }
}
