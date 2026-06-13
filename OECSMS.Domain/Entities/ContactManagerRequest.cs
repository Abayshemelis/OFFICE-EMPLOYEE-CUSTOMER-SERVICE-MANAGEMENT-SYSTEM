using System;
using OECSMS.Domain.Enums;

namespace OECSMS.Domain.Entities
{
    public class ContactManagerRequest
    {
        public int ContactRequestId { get; set; }
        public int RequestId { get; set; }
        public string CustomerMessage { get; set; } = string.Empty;
        public string? AssistantNote { get; set; }
        public DateTime? ForwardedAt { get; set; }
        public string? ManagerReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public ContactRequestStatus Status { get; set; } = ContactRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}
