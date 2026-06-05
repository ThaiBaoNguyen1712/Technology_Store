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
        public async Task<IActionResult> RecommendHomepage([FromQuery] int limit = 15)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetHomepageRecommend(userId, limit);
            return Ok(result);
        }

        [HttpGet("detail/{productSysId}")]
        public async Task<IActionResult> RecommendDetail(string productSysId, [FromQuery] int limit = 15)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetSceneRecommend(userId, "detail", productSysId, limit);
            return Ok(result);
        }

        [HttpGet("cart")]
        public async Task<IActionResult> RecommendCart([FromQuery] int limit = 15)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetSceneRecommend(userId, "cart", null, limit);
            return Ok(result);
        }

        [HttpGet("wishlist")]
        public async Task<IActionResult> RecommendWishlist([FromQuery] int limit = 15)
        {
            int userId = GetCurrentUserId();
            var result = await _recommendServices.GetSceneRecommend(userId, "wishlist", null, limit);
            return Ok(result);
        }

        [HttpGet("scene")]
        public async Task<IActionResult> RecommendByScene([FromQuery] string scene, [FromQuery] string? productSysId, [FromQuery] int topN = 15)
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
