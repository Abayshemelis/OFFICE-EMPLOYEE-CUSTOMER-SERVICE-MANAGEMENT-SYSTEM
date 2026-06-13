using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Application.DTOs;
using OECSMS.Application.Interfaces;

namespace OECSMS.API.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("users")]
    public class UsersController : BaseApiController
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role, [FromQuery] bool? isActive)
        {
            var users = await _authService.GetUsersAsync(role, isActive);
            return ApiResponse(users, "Users list retrieved successfully.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var users = await _authService.GetUsersAsync();
            var user = users.GetEnumerator();
            // Simple lookup
            foreach (var u in users)
            {
                if (u.UserId == id)
                {
                    return ApiResponse(u, "User details retrieved.");
                }
            }
            return ApiError("User not found", 404);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAssistant([FromBody] RegisterUserRequest request)
        {
            // Set role to Assistant explicitly if manager tries to create a user
            request.Role = "Assistant";
            var success = await _authService.RegisterAsync(request, managerId: CurrentUserId);
            if (!success)
            {
                return ApiError("Username already exists or invalid registration details.");
            }

            return ApiResponse<object?>(null, "Assistant account created successfully.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateUserRequest request)
        {
            var success = await _authService.UpdateProfileAsync(id, request);
            if (!success)
            {
                return ApiError("User not found or update failed.");
            }

            return ApiResponse<object?>(null, "User profile updated successfully.");
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            var success = await _authService.ToggleUserStatusAsync(id, isActive);
            if (!success)
            {
                return ApiError("User not found.");
            }

            var statusStr = isActive ? "activated" : "deactivated";
            return ApiResponse<object?>(null, $"User has been successfully {statusStr}.");
        }

        [HttpGet("{id}/performance")]
        public async Task<IActionResult> GetPerformanceReport(int id)
        {
            var report = await _authService.GetPerformanceReportAsync(id);
            return ApiResponse(report, "Performance report generated.");
        }
    }
}
