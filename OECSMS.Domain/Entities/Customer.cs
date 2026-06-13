using System;
using System.Collections.Generic;

namespace OECSMS.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;
        public int QueueNumber { get; set; }

        // Navigation properties
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
