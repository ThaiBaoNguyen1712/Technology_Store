using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tech_Store.Models;
using Tech_Store.Models.Constants;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Client;

public class BannerQueryService : IBannerQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private const string CacheVersionKey = "banner-cache-version";

    public BannerQueryService(ApplicationDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    public async Task<IReadOnlyList<BannerRenderItemViewModel>> GetBannersAsync(string positionCode, int? categoryId = null, int? brandId = null, int take = 1)
    {
        var cacheVersion = _memoryCache.Get<int?>(CacheVersionKey) ?? 1;
        var cacheKey = $"banner-zone:{cacheVersion}:{positionCode}:{categoryId?.ToString() ?? "all"}:{brandId?.ToString() ?? "all"}:{take}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<BannerRenderItemViewModel>? cached) && cached != null)
        {
            return cached;
        }

        var now = DateTime.Now;
        var maps = await _context.BannerPositionMaps
            .AsNoTracking()
            .Include(x => x.Banner)
                .ThenInclude(x => x.BannerTarget)
                    .ThenInclude(x => x!.Category)
            .Include(x => x.Banner)
                .ThenInclude(x => x.BannerTarget)
                    .ThenInclude(x => x!.Brand)
            .Include(x => x.Banner)
                .ThenInclude(x => x.BannerTarget)
                    .ThenInclude(x => x!.Product)
            .Include(x => x.BannerPosition)
            .Include(x => x.DisplayBrand)
            .Where(x =>
                x.BannerPosition.Code == positionCode &&
                x.BannerPosition.IsActive &&
                x.IsActive &&
                x.Banner.IsActive &&
                !x.Banner.IsDeleted &&
                (!x.StartAt.HasValue || x.StartAt <= now) &&
                (!x.EndAt.HasValue || x.EndAt >= now) &&
                (!x.DisplayCategoryId.HasValue || x.DisplayCategoryId == categoryId) &&
                (!x.DisplayBrandId.HasValue || x.DisplayBrandId == brandId))
            .ToListAsync();

        var rankedMaps = maps
            .Select(x => new
            {
                Map = x,
                ContextScore =
                    (brandId.HasValue && x.DisplayBrandId == brandId.Value ? 500 : 0) +
                    (brandId.HasValue && x.Banner.BannerTarget?.TargetType == "brand" && x.Banner.BannerTarget.BrandId == brandId.Value ? 400 : 0) +
                    (categoryId.HasValue && x.Banner.BannerTarget?.TargetType == "category" && x.Banner.BannerTarget.CategoryId == categoryId.Value ? 300 : 0) +
                    (x.DisplayCategoryId.HasValue && categoryId.HasValue && x.DisplayCategoryId == categoryId.Value ? 200 : 0) +
                    (!x.DisplayCategoryId.HasValue ? 100 : 0)
            })
            .OrderByDescending(x => x.ContextScore)
            .ThenByDescending(x => x.Map.Priority)
            .ThenByDescending(x => x.Map.BannerPositionMapId)
            .Select(x => x.Map)
            .ToList();

        var activeMaps = rankedMaps.Where(x => !x.IsDefault).ToList();
        var defaultMaps = rankedMaps.Where(x => x.IsDefault).ToList();

        var selectedMaps = activeMaps.Take(take).ToList();
        if (selectedMaps.Count < take)
        {
            foreach (var fallback in defaultMaps)
            {
                if (selectedMaps.Any(x => x.BannerId == fallback.BannerId))
                {
                    continue;
                }

                selectedMaps.Add(fallback);
                if (selectedMaps.Count >= take)
                {
                    break;
                }
            }
        }

        var result = selectedMaps
            .Select(x => new BannerRenderItemViewModel
            {
                BannerId = x.BannerId,
                Name = x.Banner.Name,
                DesktopImageUrl = x.Banner.DesktopImageUrl,
                MobileImageUrl = x.Banner.MobileImageUrl,
                AltText = string.IsNullOrWhiteSpace(x.Banner.AltText) ? x.Banner.Name : x.Banner.AltText!,
                NavigateUrl = $"/banner/go/{x.BannerId}",
                OpenInNewTab = x.Banner.BannerTarget?.OpenInNewTab ?? false,
                Priority = x.Priority
            })
            .ToList();

        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<string> ResolveNavigationUrlAsync(int bannerId)
    {
        var target = await _context.BannerTargets
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.BannerId == bannerId);

        if (target == null)
        {
            return "/";
        }

        return target.TargetType switch
        {
            "category" when !string.IsNullOrWhiteSpace(target.Category?.EngTitle) => $"/Category/{target.Category.EngTitle}",
            "brand" when !string.IsNullOrWhiteSpace(target.Brand?.Name) => $"/Brand/{Uri.EscapeDataString(target.Brand.Name)}",
            "product" when !string.IsNullOrWhiteSpace(target.Product?.Slug) => $"/View/{target.Product.Slug}",
            _ => string.IsNullOrWhiteSpace(target.ExternalUrl) ? "/" : target.ExternalUrl!
        };
    }

    public async Task<bool> ShouldOpenInNewTabAsync(int bannerId)
    {
        return await _context.BannerTargets
            .AsNoTracking()
            .Where(x => x.BannerId == bannerId)
            .Select(x => x.OpenInNewTab)
            .FirstOrDefaultAsync();
    }
}
