using System;
using OECSMS.Domain.Enums;

namespace OECSMS.Domain.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int RecipientUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? RelatedEntityId { get; set; }

        // Navigation properties
        public User RecipientUser { get; set; } = null!;
    }
}
