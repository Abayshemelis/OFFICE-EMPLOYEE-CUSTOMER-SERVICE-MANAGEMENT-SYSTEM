using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Application.DTOs;
using OECSMS.Application.Interfaces;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return ApiError("Invalid username or password, or account is deactivated.");
            }

            return ApiResponse(response, "Login successful.");
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Simple response since tokens are stateless. Client clears JWT token local storage.
            return ApiResponse<object?>(null, "Logged out successfully.");
        }

        [HttpPost("refresh")]
        [Authorize]
        public IActionResult Refresh()
        {
            // For stateless tokens, we can return the current user's profile or refresh details.
            return ApiResponse(new { status = "token_valid" }, "Token validated successfully.");
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var success = await _authService.ChangePasswordAsync(CurrentUserId, request);
            if (!success)
            {
                return ApiError("Failed to change password. Verify your old password.");
            }

            return ApiResponse<object?>(null, "Password changed successfully.");
        }
    }
}
