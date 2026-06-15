using System;
using System.ComponentModel.DataAnnotations;

namespace OECSMS.Domain.Entities
{
    public class ContactManagerRequest
    {
        [Key]
        public int Id { get; set; }

        // Reference to the customer who created the request
        public int CustomerId { get; set; }

        // The message the customer typed
        [Required]
        public string Message { get; set; } = string.Empty;

        // Current status of the request
        public ContactRequestStatus Status { get; set; } = ContactRequestStatus.Pending;

        // Optional note added by the assistant before forwarding
        public string? AssistantNote { get; set; }

        // Reply from the manager (optional)
        public string? ManagerReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum ContactRequestStatus
    {
        Pending,
        Forwarded,
        Replied,
        Closed
    }
}
