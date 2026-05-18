using Microsoft.AspNetCore.Mvc;
using Tech_Store.Services.Client;

namespace Tech_Store.Controllers;

[Route("banner")]
public class BannerController : Controller
{
    private readonly IBannerQueryService _bannerQueryService;

    public BannerController(IBannerQueryService bannerQueryService)
    {
        _bannerQueryService = bannerQueryService;
    }

    [HttpGet("go/{bannerId:int}")]
    public async Task<IActionResult> Go(int bannerId)
    {
        var targetUrl = await _bannerQueryService.ResolveNavigationUrlAsync(bannerId);
        return Redirect(targetUrl);
    }
}
