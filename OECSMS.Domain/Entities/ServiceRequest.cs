using System;
using System.Collections.Generic;
using OECSMS.Domain.Enums;

namespace OECSMS.Domain.Entities
{
    public class ServiceRequest
    {
        public int RequestId { get; set; }
        public int CustomerId { get; set; }
        public int AssistantId { get; set; }
        public string ServiceDescription { get; set; } = string.Empty;
        public RequestStatus Status { get; set; } = RequestStatus.Waiting;
        public DateTime? ServiceStartTime { get; set; }
        public DateTime? ServiceEndTime { get; set; }
        public string? ResolutionNote { get; set; }
        public byte? CustomerRating { get; set; }
        public string? CustomerFeedback { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Customer Customer { get; set; } = null!;
        public User Assistant { get; set; } = null!;
        public ICollection<ContactManagerRequest> ContactManagerRequests { get; set; } = new List<ContactManagerRequest>();
        public ICollection<AssistantConductScore> ConductScores { get; set; } = new List<AssistantConductScore>();
    }
}
