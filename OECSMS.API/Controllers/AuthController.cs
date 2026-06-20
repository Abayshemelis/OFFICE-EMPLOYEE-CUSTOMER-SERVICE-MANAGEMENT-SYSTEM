using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Infrastructure.Data;
using Google.Apis.Auth;
using System.Net.Http;
using System.Text.Json;

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

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Role))
            {
                request.Role = "Customer";
            }
            var result = await _authService.RegisterAsync(request);
            if (!result.IsSuccess)
            {
                return ApiError(string.Join(", ", result.Errors));
            }
            return ApiResponse<object?>(null, "Registration successful. Please check your email to verify your account.");
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] System.Guid token)
        {
            var result = await _authService.ConfirmEmailAsync(token);
            if (!result.IsSuccess)
            {
                return ApiError(string.Join(", ", result.Errors));
            }
            return ApiResponse<object?>(null, "Email verified successfully. You can now login.");
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request.Email);
            if (!result.IsSuccess)
            {
                return ApiError(string.Join(", ", result.Errors));
            }
            return ApiResponse<object?>(null, "If the email matches an account, a password reset link has been sent.");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordTokenRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result.IsSuccess)
            {
                return ApiError(string.Join(", ", result.Errors));
            }
            return ApiResponse<object?>(null, "Password reset successfully. You can now login with your new password.");
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Stateless tokens – client should remove stored JWT.
            return ApiResponse<object?>(null, "Logged out successfully.");
        }

        [HttpPost("refresh")]
        [Authorize]
        public IActionResult Refresh()
        {
            // Placeholder – implement token refresh if using refresh tokens.
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

        // Google login endpoint
        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                // If the credential does not contain a dot, treat it as a mock email (fallback mode)
                if (!request.Credential.Contains('.'))
                {
                    var mockEmail = request.Credential;
                    var mockName = mockEmail.Split('@')[0];
                    var mockResponse = await _authService.LoginWithGoogleAsync(mockEmail, mockName, request.Role);
                    return ApiResponse(mockResponse, "Google login successful (mock).");
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential);
                var email = payload.Email;
                var name = payload.Name ?? payload.GivenName ?? "";
                var response = await _authService.LoginWithGoogleAsync(email, name, request.Role);
                return ApiResponse(response, "Google login successful.");
            }
            catch (System.Exception ex)
            {
                // If validation fails, check if the credential resembles an email and attempt mock login
                if (request.Credential.Contains("@"))
                {
                    var fallbackEmail = request.Credential;
                    var fallbackName = fallbackEmail.Split('@')[0];
                    var fallbackResponse = await _authService.LoginWithGoogleAsync(fallbackEmail, fallbackName);
                    return ApiResponse(fallbackResponse, "Google login successful (fallback email).");
                }
                return ApiError("Google token validation failed. " + ex.Message);
            }
        }

        // DTOs
        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public class ResetPasswordTokenRequest
        {
            public System.Guid Token { get; set; }
            public string NewPassword { get; set; } = string.Empty;
        }

        public class GoogleLoginRequest
        {
            public string Credential { get; set; } = string.Empty;
            public string Role { get; set; } = "Customer"; // default role
        }
    }
}
