using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Stock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChaosController : ControllerBase
    {
        // Simple static state for the demo
        public static bool IsLatencyEnabled = false;
        public static bool IsFailureEnabled = false;

        [HttpPost("toggle-latency")]
        public IActionResult ToggleLatency([FromQuery] bool enable)
        {
            IsLatencyEnabled = enable;
            return Ok(new { Message = $"Latency Mode is now {(enable ? "ENABLED (2s delay)" : "DISABLED")}" });
        }

        [HttpPost("toggle-failure")]
        public IActionResult ToggleFailure([FromQuery] bool enable)
        {
            IsFailureEnabled = enable;
            return Ok(new { Message = $"Failure Mode is now {(enable ? "ENABLED (Throw 500)" : "DISABLED")}" });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { IsLatencyEnabled, IsFailureEnabled });
        }
    }
}
