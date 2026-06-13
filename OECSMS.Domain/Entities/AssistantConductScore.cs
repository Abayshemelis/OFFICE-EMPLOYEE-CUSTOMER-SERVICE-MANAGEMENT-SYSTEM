using System;

namespace OECSMS.Domain.Entities
{
    public class AssistantConductScore
    {
        public int ScoreId { get; set; }
        public int AssistantId { get; set; }
        public int RequestId { get; set; }
        public byte Rating { get; set; }
        public string? ManagerNote { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User Assistant { get; set; } = null!;
        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}
