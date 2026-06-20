using System;

namespace OECSMS.Contracts.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? Phone { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class Result
    {
        public bool IsSuccess { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();

        public static Result Success() => new Result { IsSuccess = true };
        public static Result Fail(params string[] errors) => new Result { IsSuccess = false, Errors = errors };
    }

    public class ResetPasswordRequest
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PerformanceReportDTO
    {
        public int AssistantId { get; set; }
        public int TasksCompleted { get; set; }
        public int CustomersServed { get; set; }
        public double AverageRating { get; set; }
    }
}
