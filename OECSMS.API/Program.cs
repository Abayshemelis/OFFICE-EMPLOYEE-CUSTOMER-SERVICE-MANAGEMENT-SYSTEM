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
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://0.0.0.0:5000"); // Disabled to avoid port conflicts; Kestrel will choose a free port

// Add services to the container.
builder.Services.AddControllers();


var jwtKey = builder.Configuration["Jwt:Key"] ?? "SecretKeyForOECSMSSystemAuthentication2026";
var key = Encoding.ASCII.GetBytes(jwtKey);
// Database Configuration – MySQL with graceful fallback to SQLite when no connection string is provided
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    // Use MySQL; Pomelo will auto‑detect the server version
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
            mysql => mysql.EnableRetryOnFailure()));
}
else
{
    // Demo‑mode fallback: use a lightweight SQLite file database
    var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "oeccms_demo.db");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={sqlitePath}"));
}

// Google OAuth client ID/secret validation will be logged at runtime if missing
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
})
.AddOpenIdConnect(options =>
{
    options.Authority = "https://accounts.google.com";
    options.ClientId = builder.Configuration["GoogleOAuth:ClientId"];
    options.ClientSecret = builder.Configuration["GoogleOAuth:ClientSecret"];
    options.ResponseType = "code";
    // Scopes from config (default set if missing)
    options.Scope.Clear();
    var defaultScopes = new[] { "openid", "profile", "email" };
    var configured = builder.Configuration.GetSection("GoogleOAuth:Scopes").Get<string[]>() ?? defaultScopes;
    foreach (var s in configured) options.Scope.Add(s);
    options.CallbackPath = builder.Configuration["GoogleOAuth:CallbackPath"];
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "accounts.google.com",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["GoogleOAuth:ClientId"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    // Add diagnostic events
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var email = ctx.Principal?.FindFirst("email")?.Value ?? "unknown";
            logger.LogInformation("Google token validated for {Email}", email);
            return System.Threading.Tasks.Task.CompletedTask;
        },
        OnRemoteFailure = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ctx.Failure, "Google authentication failed");
            // Redirect to a simple error endpoint (you can change as needed)
            var errorMsg = ctx.Failure?.Message ?? "Unknown error";
            ctx.Response.Redirect($"/auth/google-failure?msg={Uri.EscapeDataString(errorMsg)}");
            
            ctx.HandleResponse();
            return System.Threading.Tasks.Task.CompletedTask;
        }
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
app.MapGet("/health", async (AppDbContext db) => {
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
        {
            return Results.Json(new { status = "Degraded", message = "❌ Database is unreachable" }, statusCode: 503);
        }
        return Results.Ok(new { status = "OK", message = "✅ OECSMS API and Database are running" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "Error", message = ex.Message }, statusCode: 503);
    }
});

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
            // Seed a default manager with explicit ID
            var defaultManager = new User
            {
                UserId = 1,
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

            // Seed a default assistant with explicit ID and link to manager
            var defaultAssistant = new User
            {
                UserId = 2,
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
