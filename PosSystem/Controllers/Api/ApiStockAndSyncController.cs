using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace PosSystem.Controllers.Api
{


    [Route("api/sync")]
    [ApiController]
    [Authorize]
    public class ApiSyncController : ControllerBase
    {
        [HttpPost("push")]
        public IActionResult PushOfflineData([FromBody] object dto)
        {
            return Ok(new { success = true, data = true, message = "สำเร็จ" });
        }

        [HttpGet("pull")]
        public IActionResult PullDataForOffline(Guid branchId)
        {
            return Ok(new { success = true, data = new { products = new object[] { }, categories = new object[] { } }, message = "สำเร็จ" });
        }

        [HttpGet("status")]
        public IActionResult GetSyncStatus(string deviceId)
        {
            return Ok(new { success = true, data = "SYNCED", message = "สำเร็จ" });
        }
    }
}
