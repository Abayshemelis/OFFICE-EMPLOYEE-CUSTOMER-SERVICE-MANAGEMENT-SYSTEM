using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OECSMS.Domain.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Manager" or "Assistant"
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public int? ManagerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public User? Manager { get; set; }
        public ICollection<User> Assistants { get; set; } = new List<User>();
        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
        public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
        public ICollection<ServiceRequest> HandledRequests { get; set; } = new List<ServiceRequest>();
        public ICollection<AssistantConductScore> ConductScores { get; set; } = new List<AssistantConductScore>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
