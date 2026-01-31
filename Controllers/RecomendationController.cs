using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tech_Store.Models;
using Tech_Store.Services.Client.RecommendServices;

namespace Tech_Store.Controllers
{
    [ApiController]
    [Route("api/v1/recommendations")]
    public class RecomendationController : BaseController
    {
        private readonly RecommendServices _recommendServices;

        public RecomendationController( ApplicationDbContext context, RecommendServices recommendServices) : base(context)
        {
            _recommendServices = recommendServices;
        }

        [HttpGet("homepage")]
        public async Task<IActionResult> RecommendHomepage([FromQuery] int topN = 50)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetHomepageRecommend(userId, topN);
            return Ok(result);
        }

        [HttpGet("scene")]
        public async Task<IActionResult> RecommendByScene([FromQuery] string scene, [FromQuery] string? productSysId, [FromQuery] int topN = 10)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetSceneRecommend(userId, scene, productSysId, topN);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimId, out int id) && id > 0) return id;

            var rawGuestId = HttpContext.Items["guest_id"]?.ToString() ?? HttpContext.Request.Cookies["guest_id"];
            return int.TryParse(rawGuestId, out var guestId) ? guestId : 0;
        }
    }
}
