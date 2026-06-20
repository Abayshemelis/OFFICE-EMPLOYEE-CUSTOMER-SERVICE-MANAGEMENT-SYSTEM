using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using OECSMS.Infrastructure.Data;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace OECSMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IContactManagerRequestRepository _contactRequestRepository;
        private readonly IAssistantConductScoreRepository _conductScoreRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly AppDbContext _dbContext;

        public AuthService(
            IUserRepository userRepository,
            ITaskRepository taskRepository,
            IServiceRequestRepository serviceRequestRepository,
            IContactManagerRequestRepository contactRequestRepository,
            IAssistantConductScoreRepository conductScoreRepository,
            IConfiguration configuration,
            IEmailService emailService,
            AppDbContext dbContext)
        {
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _contactRequestRepository = contactRequestRepository;
            _conductScoreRepository = conductScoreRepository;
            _configuration = configuration;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !user.IsActive)
                return null;

            if (!user.EmailConfirmed)
                return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                return null;

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                UserId = user.UserId
            };
        }

        public async Task<LoginResponse?> LoginWithGoogleAsync(string email, string fullName, string role)
        {
            // Try to find existing user by email
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Auto-register a new user: Google has already verified the email
                var username = email.Split('@')[0].ToLowerInvariant().Replace(".", "_");
                // Ensure unique username by appending a number if needed
                var baseUsername = username;
                int suffix = 1;
                while (await _userRepository.GetByUsernameAsync(username) != null)
                {
                    username = baseUsername + suffix++;
                }

                // Default role to Customer if not provided
                var assignedRole = string.IsNullOrWhiteSpace(role) ? "Customer" : role;

                user = new User
                {
                    Username = username,
                    // Random password since they log in via Google; they can set one via Forgot Password
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 12),
                    FullName = fullName,
                    Role = assignedRole,
                    Email = email,
                    IsActive = true,
                    EmailConfirmed = true,   // Google already confirmed this email
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
            }
            else if (!user.IsActive)
            {
                return null; // Deactivated account
            }
            else
            {
                // Ensure email is confirmed for existing users (they may have registered but not confirmed)
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                }
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            var jwtToken = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = jwtToken,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                UserId = user.UserId
            };
        }
        {
// Duplicate LoginWithGoogleAsync overload removed – role handling is in the method above
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Auto-register a new user: Google has already verified the email
                var username = email.Split('@')[0].ToLowerInvariant().Replace(".", "_");
                // Ensure unique username by appending a number if needed
                var baseUsername = username;
                int suffix = 1;
                while (await _userRepository.GetByUsernameAsync(username) != null)
                {
                    username = baseUsername + suffix++;
                }

                user = new User
                {
                    Username = username,
                    // Random password since they log in via Google; they can set one via Forgot Password
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 12),
                    FullName = fullName,
// Duplicate overload code removed
                    Email = email,
                    IsActive = true,
                    EmailConfirmed = true,   // Google already confirmed this email
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
            }
            else if (!user.IsActive)
            {
                return null; // Deactivated account
            }
            else
            {
                // Ensure email is confirmed for existing users (they may have registered but not confirmed)
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                }
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            var jwtToken = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = jwtToken,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                UserId = user.UserId
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<Result> RegisterAsync(RegisterUserRequest request, int? managerId = null)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null) return Result.Fail("Username already exists.");

            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null) return Result.Fail("Email already registered.");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                FullName = request.FullName,
                Role = request.Role,
                Email = request.Email,
                Phone = request.Phone,
                IsActive = true,
                EmailConfirmed = false,
                ManagerId = managerId,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            var token = new EmailVerification
            {
                UserId = user.UserId,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            await _dbContext.EmailVerifications.AddAsync(token);
            await _dbContext.SaveChangesAsync();

            var verifyUrl = $"http://localhost:5000/verify.html?token={token.Token}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Verify your OECSMS Account",
                $"Hello {user.FullName},\n\nPlease verify your account by clicking the link below:\n{verifyUrl}\n\nThank you!"
            );

            return Result.Success();
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Phone = request.Phone;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = isActive;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<IEnumerable<LoginResponse>> GetUsersAsync(string? role = null, bool? isActive = null)
        {
            var users = await _userRepository.GetAllAsync(role, isActive);
            return users.Select(u => new LoginResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role
            });
        }

        public async Task<UserPerformanceReport> GetPerformanceReportAsync(int assistantId)
        {
            var assistant = await _userRepository.GetByIdAsync(assistantId);
            if (assistant == null) throw new ArgumentException("Assistant not found");

            var tasks = await _taskRepository.GetAllAsync(assigneeId: assistantId);
            var completedTasks = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Completed);
            
            // Check overdue
            var overdueTasks = tasks.Count(t => t.Status != Domain.Enums.TaskStatus.Completed && t.Deadline < DateTime.UtcNow);

            var requests = await _serviceRequestRepository.GetAllAsync(assistantId: assistantId);
            var served = requests.Count(r => r.Status == Domain.Enums.RequestStatus.Completed || r.Status == Domain.Enums.RequestStatus.Referred);

            var scores = await _conductScoreRepository.GetAssistantScoresAsync(assistantId);
            var avgRating = scores.Any() ? scores.Average(s => s.Rating) : 0.0;

            var contactRequests = await _contactRequestRepository.GetAllAsync(assistantId: assistantId);
            var escalations = contactRequests.Count(c => c.Status != Domain.Enums.ContactRequestStatus.Pending);

            return new UserPerformanceReport
            {
                UserId = assistantId,
                FullName = assistant.FullName,
                TotalTasksAssigned = tasks.Count(),
                TasksCompleted = completedTasks,
                TasksOverdue = overdueTasks,
                CustomersServed = served,
                AverageRating = Math.Round(avgRating, 2),
                TotalEscalations = escalations
            };
        }
        public async Task<Result> ConfirmEmailAsync(Guid token)
        {
            var verification = await _dbContext.EmailVerifications
                .Include(ev => ev.User)
                .FirstOrDefaultAsync(ev => ev.Token == token);

            if (verification == null)
                return Result.Fail("Invalid or expired verification token.");

            if (verification.IsUsed)
                return Result.Fail("This token has already been used.");

            if (verification.ExpiresAt < DateTime.UtcNow)
                return Result.Fail("This token has expired.");

            verification.IsUsed = true;
            verification.User.EmailConfirmed = true;

            _dbContext.EmailVerifications.Update(verification);
            await _userRepository.UpdateAsync(verification.User); // saves changes

            return Result.Success();
        }

        public async Task<Result> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Return success to avoid email enumeration
                return Result.Success();
            }

            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            await _dbContext.PasswordResetTokens.AddAsync(resetToken);
            await _dbContext.SaveChangesAsync();

            var resetUrl = $"http://localhost:5000/reset-password.html?token={resetToken.Token}";
            await _emailService.SendEmailAsync(
                user.Email,
                "Reset your OECSMS Password",
                $"Hello {user.FullName},\n\nPlease reset your password by clicking the link below:\n{resetUrl}\n\nThis link will expire in 2 hours."
            );

            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(Guid token, string newPassword)
        {
            var resetToken = await _dbContext.PasswordResetTokens
                .Include(pt => pt.User)
                .FirstOrDefaultAsync(pt => pt.Token == token);

            if (resetToken == null)
                return Result.Fail("Invalid or expired password reset token.");

            if (resetToken.IsUsed)
                return Result.Fail("This token has already been used.");

            if (resetToken.ExpiresAt < DateTime.UtcNow)
                return Result.Fail("This token has expired.");

            resetToken.IsUsed = true;
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);

            _dbContext.PasswordResetTokens.Update(resetToken);
            await _userRepository.UpdateAsync(resetToken.User); // saves changes

            return Result.Success();
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? "SecretKeyForOECSMSSystemAuthentication2026";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["Jwt:Issuer"] ?? "OECSMS",
                Audience = _configuration["Jwt:Audience"] ?? "OECSMS",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
