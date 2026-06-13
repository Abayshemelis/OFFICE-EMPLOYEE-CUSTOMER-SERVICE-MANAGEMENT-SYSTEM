using Microsoft.AspNetCore.Mvc;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("home")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() =>
            Ok(new { message = "✅ OECSMS API is running. Use /api/v1/... endpoints." });
    }
}
