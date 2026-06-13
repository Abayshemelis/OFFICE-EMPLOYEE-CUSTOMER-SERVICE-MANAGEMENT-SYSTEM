using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OECSMS.Application.Interfaces;
using OECSMS.Application.Services;
using OECSMS.Domain.Entities;
using OECSMS.Infrastructure.Data;
using OECSMS.Infrastructure.Hubs;
using OECSMS.Infrastructure.Repositories;
using OECSMS.Infrastructure.Services;
using OECSMS.API.Middleware;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000"); // Explicitly bind to all interfaces on port 5000

// Add services to the container.
builder.Services.AddControllers();

// Database Configuration - Use MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SecretKeyForOECSMSSystemAuthentication2026";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OECSMS",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OECSMS",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// SignalR Configuration
builder.Services.AddSignalR();

// Dependency Injection Setup
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
builder.Services.AddScoped<IContactManagerRequestRepository, ContactManagerRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAssistantConductScoreRepository, AssistantConductScoreRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationHubContext, NotificationHubContext>();

// CORS Settings - Allow all for frontend development convenience
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Frontend")),
    EnableDefaultFiles = true,
    RequestPath = ""
});
app.MapGet("/health", () => Results.Ok(new { status = "OK", message = "✅ OECSMS API is running on port 5000" }));

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// Database Initialization and Auto-Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Ensure Database is Created
        context.Database.Migrate();

        // Check if Default Manager Account exists
        if (!context.Users.Any())
        {
            var defaultManager = new User
            {
                Username = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!", 12),
                FullName = "System Manager",
                Role = "Manager",
                Email = "manager@oecsms.com",
                Phone = "555-0100",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(defaultManager);
            context.SaveChanges();
            
            // Seed a default assistant as well for ease of demonstration
            var defaultAssistant = new User
            {
                Username = "assistant",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Assistant123!", 12),
                FullName = "Alice Assistant",
                Role = "Assistant",
                Email = "alice@oecsms.com",
                Phone = "555-0200",
                IsActive = true,
                ManagerId = defaultManager.UserId,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(defaultAssistant);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
