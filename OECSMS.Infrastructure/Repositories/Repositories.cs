using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OECSMS.Application.Interfaces;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using DomainTaskStatus = OECSMS.Domain.Enums.TaskStatus;
using OECSMS.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;
using DomTask = OECSMS.Domain.Entities.Task;

namespace OECSMS.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Manager)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<User>> GetAllAsync(string? role = null, bool? isActive = null)
        {
            IQueryable<User> query = _context.Users;

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }

    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DomTask?> GetByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .FirstOrDefaultAsync(t => t.TaskId == id);
        }

        public async Task<IEnumerable<DomTask>> GetAllAsync(
            DomainTaskStatus? status = null, 
            TaskPriority? priority = null, 
            int? assigneeId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            IQueryable<DomTask> query = _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(t => t.Priority == priority.Value);

            if (assigneeId.HasValue)
                query = query.Where(t => t.AssignedToId == assigneeId.Value);

            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(DomTask task)
        {
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DomTask task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(DomTask task)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task AddAuditLogAsync(TaskAuditLog log)
        {
            await _context.TaskAuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TaskAuditLog>> GetAuditLogsAsync(int taskId)
        {
            return await _context.TaskAuditLogs
                .Include(l => l.ChangedByUser)
                .Where(l => l.TaskId == taskId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DomTask>> GetOverdueTasksAsync()
        {
            return await _context.Tasks
                .Include(t => t.AssignedTo)
                .Where(t => t.Status != DomainTaskStatus.Completed && 
                            t.Status != DomainTaskStatus.Cancelled && 
                            t.Deadline.HasValue && 
                            t.Deadline.Value < DateTime.UtcNow)
                .ToListAsync();
        }
    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<IEnumerable<Customer>> GetActiveQueueAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Customers
                .Include(c => c.ServiceRequests)
                .Where(c => c.VisitDate == today)
                .OrderBy(c => c.QueueNumber)
                .ToListAsync();
        }

        public async Task<int> GetNextQueueNumberAsync(DateTime date)
        {
            var count = await _context.Customers
                .CountAsync(c => c.VisitDate == date);
            return count + 1;
        }

        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
    }

    public class ServiceRequestRepository : IServiceRequestRepository
    {
        private readonly AppDbContext _context;

        public ServiceRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceRequest?> GetByIdAsync(int id)
        {
            return await _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Assistant)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        public async Task<IEnumerable<ServiceRequest>> GetAllAsync(
            RequestStatus? status = null, 
            int? assistantId = null)
        {
            IQueryable<ServiceRequest> query = _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Assistant);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (assistantId.HasValue)
                query = query.Where(r => r.AssistantId == assistantId.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(ServiceRequest request)
        {
            await _context.ServiceRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ServiceRequest request)
        {
            _context.ServiceRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }

    public class ContactManagerRequestRepository : IContactManagerRequestRepository
    {
        private readonly AppDbContext _context;

        public ContactManagerRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ContactManagerRequest?> GetByIdAsync(int id)
        {
            return await _context.ContactManagerRequests
                .Include(c => c.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Include(c => c.ServiceRequest)
                    .ThenInclude(r => r.Assistant)
                .FirstOrDefaultAsync(c => c.ContactRequestId == id);
        }

        public async Task<IEnumerable<ContactManagerRequest>> GetAllAsync(
            ContactRequestStatus? status = null,
            int? assistantId = null)
        {
            IQueryable<ContactManagerRequest> query = _context.ContactManagerRequests
                .Include(c => c.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Include(c => c.ServiceRequest)
                    .ThenInclude(r => r.Assistant);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (assistantId.HasValue)
                query = query.Where(c => c.ServiceRequest.AssistantId == assistantId.Value);

            return await query.ToListAsync();
        }

        public async Task AddAsync(ContactManagerRequest request)
        {
            await _context.ContactManagerRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ContactManagerRequest request)
        {
            _context.ContactManagerRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            if (unread.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }

    public class AssistantConductScoreRepository : IAssistantConductScoreRepository
    {
        private readonly AppDbContext _context;

        public AssistantConductScoreRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AssistantConductScore>> GetAssistantScoresAsync(int assistantId)
        {
            return await _context.AssistantConductScores
                .Include(s => s.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Where(s => s.AssistantId == assistantId)
                .OrderByDescending(s => s.RecordedAt)
                .ToListAsync();
        }

        public async Task AddAsync(AssistantConductScore score)
        {
            await _context.AssistantConductScores.AddAsync(score);
            await _context.SaveChangesAsync();
        }
    }
}
