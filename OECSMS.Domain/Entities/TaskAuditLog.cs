using System;
using OECSMS.Domain.Enums;
using TaskStatus = OECSMS.Domain.Enums.TaskStatus;

namespace OECSMS.Domain.Entities
{
    public class TaskAuditLog
    {
        public int LogId { get; set; }
        public int TaskId { get; set; }
        public int ChangedByUserId { get; set; }
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public string? ChangeNote { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Task Task { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
    }
}
