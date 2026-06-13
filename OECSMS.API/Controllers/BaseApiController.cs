using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace OECSMS.API.Controllers
{
    [ApiController]

    public class BaseApiController : ControllerBase
    {
        protected int CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
            }
        }

        protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        protected IActionResult ApiResponse<T>(T data, string message = "Success", bool success = true)
        {
            return Ok(new
            {
                success,
                data,
                message
            });
        }

        protected IActionResult ApiError(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                success = false,
                data = (object?)null,
                message
            });
        }
    }
}
