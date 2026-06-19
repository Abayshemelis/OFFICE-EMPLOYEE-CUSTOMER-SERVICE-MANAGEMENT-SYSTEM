using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class GoogleAuthController : BaseApiController
    {
        // Initiates Google OAuth flow
        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = "/auth/google-callback" };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Callback endpoint after Google signs in the user
        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return ApiError("Google authentication failed.");

            var claims = result.Principal?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == "name")?.Value;

            // TODO: map/create local user, issue your own JWT, etc.
            return ApiResponse(new { Email = email, Name = name }, "Google login successful.");
        }
    }
}
