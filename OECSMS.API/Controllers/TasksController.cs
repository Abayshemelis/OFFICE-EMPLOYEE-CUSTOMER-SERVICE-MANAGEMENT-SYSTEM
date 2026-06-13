using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Application.DTOs;
using OECSMS.Application.Interfaces;
using OECSMS.Domain.Enums;
using DomainTaskStatus = OECSMS.Domain.Enums.TaskStatus;

namespace OECSMS.API.Controllers
{
    [Authorize]
    [Route("tasks")]
    public class TasksController : BaseApiController
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] DomainTaskStatus? status,
            [FromQuery] TaskPriority? priority,
            [FromQuery] int? assigneeId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            // Assistants should only see their own tasks
            if (CurrentUserRole == "Assistant")
            {
                assigneeId = CurrentUserId;
            }

            var tasks = await _taskService.GetTasksAsync(status, priority, assigneeId, fromDate, toDate);
            return ApiResponse(tasks, "Tasks list retrieved successfully.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return ApiError("Task not found.", 404);
            }

            if (CurrentUserRole == "Assistant" && task.AssignedToId != CurrentUserId)
            {
                return ApiError("You are not authorized to view this task.", 403);
            }

            return ApiResponse(task, "Task retrieved successfully.");
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var task = await _taskService.CreateTaskAsync(request, CurrentUserId);
            return ApiResponse(task, "Task created and assigned successfully.");
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            var task = await _taskService.UpdateTaskAsync(id, request);
            return ApiResponse(task, "Task updated successfully.");
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return ApiError("Task not found.", 404);
            }

            if (CurrentUserRole == "Assistant" && task.AssignedToId != CurrentUserId)
            {
                return ApiError("You are not authorized to update this task's status.", 403);
            }

            var updatedTask = await _taskService.UpdateTaskStatusAsync(id, request, CurrentUserId);
            return ApiResponse(updatedTask, "Task status updated.");
        }

        [HttpPatch("{id}/reassign")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ReassignTask(int id, [FromBody] int newAssigneeId)
        {
            var updatedTask = await _taskService.ReassignTaskAsync(id, newAssigneeId, CurrentUserId);
            return ApiResponse(updatedTask, "Task reassigned successfully.");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CancelTask(int id)
        {
            await _taskService.DeleteTaskAsync(id, CurrentUserId);
            return ApiResponse<object?>(null, "Task cancelled successfully.");
        }

        [HttpGet("{id}/audit")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetAuditLogs(int id)
        {
            var logs = await _taskService.GetAuditLogsAsync(id);
            return ApiResponse(logs, "Task audit logs retrieved.");
        }
    }
}
