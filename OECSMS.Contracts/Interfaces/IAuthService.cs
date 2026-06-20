using System.Threading.Tasks;
using System.Collections.Generic;
using OECSMS.Contracts.DTOs;

namespace OECSMS.Contracts
{
    public interface IAuthService
    {
        Task<Result> RegisterAsync(RegisterUserRequest model, int? managerId = null);
        Task<LoginResponse?> LoginAsync(LoginRequest model);
        Task<LoginResponse?> LoginWithGoogleAsync(string email, string fullName, string role);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest model);
        Task<IEnumerable<LoginResponse>> GetUsersAsync(string? role = null, bool? isActive = null);
        Task<bool> UpdateProfileAsync(int userId, UpdateUserRequest request);
        Task<bool> ToggleUserStatusAsync(int userId, bool isActive);
        Task<UserPerformanceReport> GetPerformanceReportAsync(int assistantId);
        Task<Result> ConfirmEmailAsync(System.Guid token);
        Task<Result> ForgotPasswordAsync(string email);
        Task<Result> ResetPasswordAsync(System.Guid token, string newPassword);
    }
}
