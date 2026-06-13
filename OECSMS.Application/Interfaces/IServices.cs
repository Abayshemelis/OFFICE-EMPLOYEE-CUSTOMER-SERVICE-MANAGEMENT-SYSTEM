using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OECSMS.Application.DTOs;
using OECSMS.Domain.Enums;
using Task = System.Threading.Tasks.Task;
using DomainTaskStatus = OECSMS.Domain.Enums.TaskStatus;
namespace OECSMS.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<bool> RegisterAsync(RegisterUserRequest request, int? managerId = null);
        Task<bool> UpdateProfileAsync(int userId, UpdateUserRequest request);
        Task<bool> ToggleUserStatusAsync(int userId, bool isActive);
        Task<IEnumerable<LoginResponse>> GetUsersAsync(string? role = null, bool? isActive = null);
        Task<UserPerformanceReport> GetPerformanceReportAsync(int assistantId);
    }

    public interface ITaskService
    {
        Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, int managerId);
        Task<TaskResponse?> GetTaskByIdAsync(int taskId);
        Task<IEnumerable<TaskResponse>> GetTasksAsync(
            DomainTaskStatus? status = null, 
            TaskPriority? priority = null, 
            int? assigneeId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null);
        Task<TaskResponse> UpdateTaskAsync(int taskId, UpdateTaskRequest request);
        Task<TaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request, int userId);
        Task<TaskResponse> ReassignTaskAsync(int taskId, int newAssigneeId, int managerId);
        Task DeleteTaskAsync(int taskId, int managerId);
        Task<IEnumerable<TaskAuditLogResponse>> GetAuditLogsAsync(int taskId);
        Task CheckOverdueTasksAsync();
    }

    public interface ICustomerService
    {
        Task<RegisterCustomerResponse> RegisterCustomerArrivalAsync(RegisterCustomerRequest request);
        Task<IEnumerable<ServiceRequestResponse>> GetActiveQueueAsync();
        Task<ServiceRequestResponse?> GetServiceRequestByIdAsync(int requestId);
        Task<IEnumerable<ServiceRequestResponse>> GetAllServiceRequestsAsync(RequestStatus? status = null, int? assistantId = null);
        Task<ServiceRequestResponse> UpdateServiceRequestStatusAsync(int requestId, UpdateServiceRequestStatusRequest request, int assistantId);
        Task SubmitFeedbackAsync(int requestId, SubmitFeedbackRequest request);
    }

    public interface ICommunicationService
    {
        Task<ContactManagerResponse> CreateContactRequestAsync(ContactManagerRequestRequest request);
        Task<IEnumerable<ContactManagerResponse>> GetContactRequestsAsync(ContactRequestStatus? status = null, int? assistantId = null);
        Task<ContactManagerResponse?> GetContactRequestByIdAsync(int id);
        Task<ContactManagerResponse> ForwardToManagerAsync(int id, ForwardContactRequest request, int assistantId);
        Task<ContactManagerResponse> ReplyFromManagerAsync(int id, ReplyContactRequest request, int managerId);
        Task<ContactManagerResponse> CloseRequestAsync(int id, int userId);
    }

    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int id, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task CreateNotificationAsync(int userId, string title, string message, NotificationType type, int? relatedEntityId = null);
    }
}
