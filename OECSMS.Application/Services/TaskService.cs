using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using DomainTaskStatus = OECSMS.Domain.Enums.TaskStatus;
using Task = System.Threading.Tasks.Task;
using DomTask = OECSMS.Domain.Entities.Task;

namespace OECSMS.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubContext _notificationHubContext;

        public TaskService(
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            INotificationHubContext notificationHubContext)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _notificationHubContext = notificationHubContext;
        }

        public async Task<IEnumerable<TaskResponse>> GetTasksAsync(
            DomainTaskStatus? status = null, 
            TaskPriority? priority = null, 
            int? assigneeId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            var tasks = await _taskRepository.GetAllAsync(status, priority, assigneeId, fromDate, toDate);
            return tasks.Select(MapToResponse);
        }

        public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, int managerId)
        {
            var task = new DomTask
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = DomainTaskStatus.Pending,
                AssignedToId = request.AssignedToId,
                AssignedById = managerId,
                Deadline = request.Deadline,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(task);

            // Audit Log
            await _taskRepository.AddAuditLogAsync(new TaskAuditLog
            {
                TaskId = task.TaskId,
                ChangedByUserId = managerId,
                OldStatus = null,
                NewStatus = DomainTaskStatus.Pending,
                ChangeNote = "Task Created and Assigned",
                ChangedAt = DateTime.UtcNow
            });

            // Notification
            var title = "New Task Assigned";
            var message = $"You have been assigned a new task: {task.Title}. Priority: {task.Priority}.";
            await _notificationService.CreateNotificationAsync(task.AssignedToId, title, message, NotificationType.TaskUpdate, task.TaskId);
            await _notificationHubContext.SendNotificationToUserAsync(task.AssignedToId, title, message, "TaskUpdate", task.TaskId);

            var dbTask = await _taskRepository.GetByIdAsync(task.TaskId);
            return MapToResponse(dbTask!);
        }

        public async Task<TaskResponse?> GetTaskByIdAsync(int taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            return task != null ? MapToResponse(task) : null;
        }

        public async Task<TaskResponse> UpdateTaskAsync(int taskId, UpdateTaskRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) throw new ArgumentException("Task not found");

            task.Title = request.Title;
            task.Description = request.Description;
            task.Priority = request.Priority;
            task.Deadline = request.Deadline;
            task.Notes = request.Notes;

            await _taskRepository.UpdateAsync(task);
            return MapToResponse(task);
        }

        public async Task<TaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request, int userId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) throw new ArgumentException("Task not found");

            var oldStatus = task.Status;
            task.Status = request.Status;

            if (request.Status == DomainTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAt = null;
            }

            await _taskRepository.UpdateAsync(task);

            // Audit log
            await _taskRepository.AddAuditLogAsync(new TaskAuditLog
            {
                TaskId = task.TaskId,
                ChangedByUserId = userId,
                OldStatus = oldStatus,
                NewStatus = request.Status,
                ChangeNote = request.Note ?? $"Status updated to {request.Status}",
                ChangedAt = DateTime.UtcNow
            });

            // Notify Manager
            var title = "Task Status Updated";
            var message = $"Task '{task.Title}' status changed from {oldStatus} to {request.Status} by {task.AssignedTo?.FullName ?? "Assistant"}.";
            await _notificationService.CreateNotificationAsync(task.AssignedById, title, message, NotificationType.TaskUpdate, task.TaskId);
            await _notificationHubContext.SendNotificationToUserAsync(task.AssignedById, title, message, "TaskUpdate", task.TaskId);
            await _notificationHubContext.SendTaskUpdateToManagerAsync(task.AssignedById, task.TaskId, request.Status.ToString());

            return MapToResponse(task);
        }

        public async Task<TaskResponse> ReassignTaskAsync(int taskId, int newAssigneeId, int managerId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) throw new ArgumentException("Task not found");

            var oldAssigneeId = task.AssignedToId;
            task.AssignedToId = newAssigneeId;
            task.Status = DomainTaskStatus.Pending;

            await _taskRepository.UpdateAsync(task);

            // Audit
            await _taskRepository.AddAuditLogAsync(new TaskAuditLog
            {
                TaskId = task.TaskId,
                ChangedByUserId = managerId,
                OldStatus = task.Status,
                NewStatus = DomainTaskStatus.Pending,
                ChangeNote = $"Task reassigned from User {oldAssigneeId} to {newAssigneeId}",
                ChangedAt = DateTime.UtcNow
            });

            // Notify new Assignee
            var title = "Task Assigned (Reassigned)";
            var message = $"A task has been reassigned to you: {task.Title}.";
            await _notificationService.CreateNotificationAsync(newAssigneeId, title, message, NotificationType.TaskUpdate, task.TaskId);
            await _notificationHubContext.SendNotificationToUserAsync(newAssigneeId, title, message, "TaskUpdate", task.TaskId);

            // Notify old Assignee
            var oldTitle = "Task Removed";
            var oldMessage = $"Task '{task.Title}' has been reassigned to another assistant.";
            await _notificationService.CreateNotificationAsync(oldAssigneeId, oldTitle, oldMessage, NotificationType.TaskUpdate, task.TaskId);
            await _notificationHubContext.SendNotificationToUserAsync(oldAssigneeId, oldTitle, oldMessage, "TaskUpdate", task.TaskId);

            var dbTask = await _taskRepository.GetByIdAsync(task.TaskId);
            return MapToResponse(dbTask!);
        }

        public async Task DeleteTaskAsync(int taskId, int managerId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) throw new ArgumentException("Task not found");

            task.Status = DomainTaskStatus.Cancelled;
            await _taskRepository.UpdateAsync(task);

            await _taskRepository.AddAuditLogAsync(new TaskAuditLog
            {
                TaskId = task.TaskId,
                ChangedByUserId = managerId,
                OldStatus = task.Status,
                NewStatus = DomainTaskStatus.Cancelled,
                ChangeNote = "Task deleted/cancelled by manager.",
                ChangedAt = DateTime.UtcNow
            });

            var title = "Task Cancelled";
            var message = $"Task '{task.Title}' has been cancelled by the manager.";
            await _notificationService.CreateNotificationAsync(task.AssignedToId, title, message, NotificationType.TaskUpdate, task.TaskId);
            await _notificationHubContext.SendNotificationToUserAsync(task.AssignedToId, title, message, "TaskUpdate", task.TaskId);
        }

        public async Task<IEnumerable<TaskAuditLogResponse>> GetAuditLogsAsync(int taskId)
        {
            var logs = await _taskRepository.GetAuditLogsAsync(taskId);
            return logs.Select(l => new TaskAuditLogResponse
            {
                LogId = l.LogId,
                TaskId = l.TaskId,
                ChangedByName = l.ChangedByUser?.FullName ?? "Unknown",
                OldStatus = l.OldStatus,
                NewStatus = l.NewStatus,
                ChangeNote = l.ChangeNote,
                ChangedAt = l.ChangedAt
            });
        }

        public async Task CheckOverdueTasksAsync()
        {
            var overdue = await _taskRepository.GetOverdueTasksAsync();
            foreach (var task in overdue)
            {
                // Create alert for Manager
                var title = "Task Overdue Alert";
                var message = $"The task '{task.Title}' assigned to {task.AssignedTo?.FullName} was due on {task.Deadline} and is now overdue.";
                
                // Avoid spamming notifications (only create if doesn't exist already or simple check)
                await _notificationService.CreateNotificationAsync(task.AssignedById, title, message, NotificationType.SystemAlert, task.TaskId);
                await _notificationHubContext.SendNotificationToUserAsync(task.AssignedById, title, message, "SystemAlert", task.TaskId);

                // Notify assistant as well
                var asstTitle = "Task Overdue Reminder";
                var asstMessage = $"Your task '{task.Title}' was due on {task.Deadline} and is marked overdue.";
                await _notificationService.CreateNotificationAsync(task.AssignedToId, asstTitle, asstMessage, NotificationType.SystemAlert, task.TaskId);
                await _notificationHubContext.SendNotificationToUserAsync(task.AssignedToId, asstTitle, asstMessage, "SystemAlert", task.TaskId);
            }
        }

        private TaskResponse MapToResponse(DomTask task)
        {
            return new TaskResponse
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                AssignedToId = task.AssignedToId,
                AssignedToName = task.AssignedTo?.FullName ?? "Unknown",
                AssignedById = task.AssignedById,
                AssignedByName = task.AssignedBy?.FullName ?? "Unknown",
                Deadline = task.Deadline,
                CompletedAt = task.CompletedAt,
                CreatedAt = task.CreatedAt,
                Notes = task.Notes
            };
        }
    }
}
