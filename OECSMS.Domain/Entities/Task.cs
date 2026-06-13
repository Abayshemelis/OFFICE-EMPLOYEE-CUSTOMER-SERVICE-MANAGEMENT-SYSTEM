using System;
using System.Collections.Generic;
using OECSMS.Domain.Enums;
using TaskStatus = OECSMS.Domain.Enums.TaskStatus;

namespace OECSMS.Domain.Entities
{
    public class Task
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public int AssignedToId { get; set; }
        public int AssignedById { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        // Navigation properties
        public User AssignedTo { get; set; } = null!;
        public User AssignedBy { get; set; } = null!;
        public ICollection<TaskAuditLog> AuditLogs { get; set; } = new List<TaskAuditLog>();
    }
}
