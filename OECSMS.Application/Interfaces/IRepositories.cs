using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using Task = System.Threading.Tasks.Task;
using DomTask = OECSMS.Domain.Entities.Task;
using DomainTaskStatus = OECSMS.Domain.Enums.TaskStatus;

namespace OECSMS.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync(string? role = null, bool? isActive = null);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }

    public interface ITaskRepository
    {
        Task<DomTask?> GetByIdAsync(int id);
        Task<IEnumerable<DomTask>> GetAllAsync(
            DomainTaskStatus? status = null, 
            TaskPriority? priority = null, 
            int? assigneeId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null);
        Task AddAsync(DomTask task);
        Task UpdateAsync(DomTask task);
        Task DeleteAsync(DomTask task);
        Task AddAuditLogAsync(TaskAuditLog log);
        Task<IEnumerable<TaskAuditLog>> GetAuditLogsAsync(int taskId);
        Task<IEnumerable<DomTask>> GetOverdueTasksAsync();
    }

    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> GetActiveQueueAsync();
        Task<int> GetNextQueueNumberAsync(DateTime date);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
    }

    public interface IServiceRequestRepository
    {
        Task<ServiceRequest?> GetByIdAsync(int id);
        Task<IEnumerable<ServiceRequest>> GetAllAsync(
            RequestStatus? status = null, 
            int? assistantId = null);
        Task AddAsync(ServiceRequest request);
        Task UpdateAsync(ServiceRequest request);
    }

    public interface IContactManagerRequestRepository
    {
        Task<ContactManagerRequest?> GetByIdAsync(int id);
        Task<IEnumerable<ContactManagerRequest>> GetAllAsync(
            ContactRequestStatus? status = null,
            int? assistantId = null);
        Task AddAsync(ContactManagerRequest request);
        Task UpdateAsync(ContactManagerRequest request);
    }

    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task AddAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task MarkAllAsReadAsync(int userId);
    }

    public interface IAssistantConductScoreRepository
    {
        Task<IEnumerable<AssistantConductScore>> GetAssistantScoresAsync(int assistantId);
        Task AddAsync(AssistantConductScore score);
    }
}
