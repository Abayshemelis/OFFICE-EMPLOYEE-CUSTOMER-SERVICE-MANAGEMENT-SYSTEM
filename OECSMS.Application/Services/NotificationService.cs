using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace OECSMS.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId);
            return notifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntityId = n.RelatedEntityId
            });
        }

        public async Task MarkAsReadAsync(int id, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification != null && notification.RecipientUserId == userId)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, NotificationType type, int? relatedEntityId = null)
        {
            var notification = new Notification
            {
                RecipientUserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = relatedEntityId
            };

            await _notificationRepository.AddAsync(notification);
        }
    }
}
