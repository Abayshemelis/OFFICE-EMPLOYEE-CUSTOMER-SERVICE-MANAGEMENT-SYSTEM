using System;
using TaskStatus = OECSMS.Domain.Enums.TaskStatus;
using OECSMS.Domain.Enums;

namespace OECSMS.Contracts.DTOs
{
    // Auth DTOs
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RegisterUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Manager or Assistant
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class UserPerformanceReport
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TotalTasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksOverdue { get; set; }
        public int CustomersServed { get; set; }
        public double AverageRating { get; set; }
        public int TotalEscalations { get; set; }
    }

    // Tasks DTOs
    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public int AssignedToId { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTaskStatusRequest
    {
        public TaskStatus Status { get; set; }
        public string? Note { get; set; }
    }

    public class TaskResponse
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public int AssignedToId { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public int AssignedById { get; set; }
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class TaskAuditLogResponse
    {
        public int LogId { get; set; }
        public int TaskId { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        public string? ChangeNote { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    // Customers DTOs
    public class RegisterCustomerRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string ServiceDescription { get; set; } = string.Empty;
        public int AssignedAssistantId { get; set; }
    }

    public class RegisterCustomerResponse
    {
        public int CustomerId { get; set; }
        public int QueueNumber { get; set; }
        public int EstimatedWaitTimeMinutes { get; set; }
        public int RequestId { get; set; }
    }

    public class CustomerResponse
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int QueueNumber { get; set; }
    }

    public class ServiceRequestResponse
    {
        public int RequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int QueueNumber { get; set; }
        public int AssistantId { get; set; }
        public string AssistantName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime? ServiceStartTime { get; set; }
        public DateTime? ServiceEndTime { get; set; }
        public string? ResolutionNote { get; set; }
        public byte? CustomerRating { get; set; }
        public string? CustomerFeedback { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateServiceRequestStatusRequest
    {
        public RequestStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class SubmitFeedbackRequest
    {
        public byte Rating { get; set; } // 1-5
        public string? Feedback { get; set; }
    }

    // Communication DTOs
    public class ContactManagerRequestRequest
    {
        public int RequestId { get; set; }
        public string CustomerMessage { get; set; } = string.Empty;
    }

    public class ForwardContactRequest
    {
        public string? AssistantNote { get; set; }
    }

    public class ReplyContactRequest
    {
        public string ReplyMessage { get; set; } = string.Empty;
    }

    public class ContactManagerResponse
    {
        public int ContactRequestId { get; set; }
        public int RequestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AssistantName { get; set; } = string.Empty;
        public string CustomerMessage { get; set; } = string.Empty;
        public string? AssistantNote { get; set; }
        public DateTime? ForwardedAt { get; set; }
        public string? ManagerReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public ContactRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationResponse
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
