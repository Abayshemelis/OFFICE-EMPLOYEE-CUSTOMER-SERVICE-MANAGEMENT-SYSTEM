using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;
using OECSMS.Application.DTOs;
using OECSMS.Application.Interfaces;
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

        public AuthService(
            IUserRepository userRepository,
            ITaskRepository taskRepository,
            IServiceRequestRepository serviceRequestRepository,
            IContactManagerRequestRepository contactRequestRepository,
            IAssistantConductScoreRepository conductScoreRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _taskRepository = taskRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _contactRequestRepository = contactRequestRepository;
            _conductScoreRepository = conductScoreRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !user.IsActive)
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

        public async Task<bool> RegisterAsync(RegisterUserRequest request, int? managerId = null)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null) return false;

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                FullName = request.FullName,
                Role = request.Role,
                Email = request.Email,
                Phone = request.Phone,
                IsActive = true,
                ManagerId = managerId,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return true;
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
