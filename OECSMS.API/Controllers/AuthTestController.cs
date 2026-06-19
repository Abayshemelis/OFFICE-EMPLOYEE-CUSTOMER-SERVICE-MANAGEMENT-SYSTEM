// Controllers/AuthTestController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : ControllerBase
    {
        // Initiates Google login – redirects to Google consent screen
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            // Challenge triggers OpenIdConnect middleware
            return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/api/auth-test/google-callback"
            }, "Google");
        }

        // Callback endpoint – user is redirected here after Google sign‑in
        [HttpGet("google-callback")]
        public IActionResult GoogleCallback()
        {
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var email = User.FindFirst("email")?.Value ?? "unknown";
                var name = User.FindFirst("name")?.Value ?? "unknown";
                return Ok(new { Message = "Google sign‑in successful", Email = email, Name = name });
            }
            return Unauthorized(new { Message = "Google sign‑in failed" });
        }
    }
}
