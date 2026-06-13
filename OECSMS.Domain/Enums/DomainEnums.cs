namespace OECSMS.Domain.Enums
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        OnHold,
        Completed,
        Cancelled
    }

    public enum RequestStatus
    {
        Waiting,
        InService,
        Completed,
        Referred,
        Unresolved
    }

    public enum ContactRequestStatus
    {
        Pending,
        Forwarded,
        Replied,
        Closed
    }

    public enum NotificationType
    {
        TaskUpdate,
        CustomerArrival,
        ContactRequest,
        SystemAlert
    }
}
