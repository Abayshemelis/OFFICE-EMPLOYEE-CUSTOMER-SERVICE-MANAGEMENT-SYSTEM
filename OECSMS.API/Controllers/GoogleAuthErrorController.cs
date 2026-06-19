using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class GoogleAuthErrorController : BaseApiController
    {
        // Endpoint hit when OpenIdConnect remote failure redirects
        [HttpGet("google-failure")]
        [AllowAnonymous]
        public IActionResult GoogleFailure([FromQuery] string msg)
        {
            // Return a simple JSON payload for debugging
            return ApiError($"Google authentication error: {msg}");
        }
    }
}
