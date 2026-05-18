using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tech_Store.Models;
using Tech_Store.Models.Enums;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.BannerServices;

public class BannerAdminService : IBannerAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;
    private const string CacheVersionKey = "banner-cache-version";

    public BannerAdminService(ApplicationDbContext context, IWebHostEnvironment environment, IMemoryCache memoryCache)
    {
        _context = context;
        _environment = environment;
        _memoryCache = memoryCache;
    }

    public async Task<AdminBannerIndexViewModel> GetIndexAsync(string? keyword, int? positionId, string? targetType, string? status, int page, int pageSize)
    {
        var query = _context.Banners
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.BannerTarget)
                .ThenInclude(x => x!.Category)
            .Include(x => x.BannerTarget)
                .ThenInclude(x => x!.Brand)
            .Include(x => x.BannerTarget)
                .ThenInclude(x => x!.Product)
            .Include(x => x.BannerPositionMaps)
                .ThenInclude(x => x.BannerPosition)
            .Include(x => x.BannerPositionMaps)
                .ThenInclude(x => x.DisplayCategory)
            .Include(x => x.BannerPositionMaps)
                .ThenInclude(x => x.DisplayBrand)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalizedKeyword) ||
                (x.AltText != null && x.AltText.Contains(normalizedKeyword)) ||
                (x.BannerTarget != null && x.BannerTarget.ExternalUrl != null && x.BannerTarget.ExternalUrl.Contains(normalizedKeyword)));
        }

        if (positionId.HasValue)
        {
            query = query.Where(x => x.BannerPositionMaps.Any(map => map.BannerPositionId == positionId.Value));
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query = query.Where(x => x.BannerTarget != null && x.BannerTarget.TargetType == targetType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status switch
            {
                "active" => query.Where(x => x.IsActive),
                "inactive" => query.Where(x => !x.IsActive),
                _ => query
            };
        }

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Min(Math.Max(1, page), totalPages);

        var banners = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.BannerId)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AdminBannerIndexViewModel
        {
            Keyword = keyword,
            PositionId = positionId,
            TargetType = targetType,
            Status = status,
            Page = currentPage,
            TotalPages = totalPages,
            TotalItems = totalItems,
            QueryString = BuildQueryString(keyword, positionId, targetType, status),
            Positions = await GetPositionOptionsAsync(),
            Banners = banners.Select(x => new AdminBannerIndexItemViewModel
            {
                BannerId = x.BannerId,
                Name = x.Name,
                DesktopImageUrl = x.DesktopImageUrl,
                TargetSummary = BuildTargetSummary(x.BannerTarget),
                PositionSummary = BuildPositionSummary(x.BannerPositionMaps),
                ScheduleSummary = BuildScheduleSummary(x.BannerPositionMaps),
                HighestPriority = x.BannerPositionMaps.Any() ? x.BannerPositionMaps.Max(map => map.Priority) : 0,
                IsActive = x.IsActive,
                HasDefaultPosition = x.BannerPositionMaps.Any(map => map.IsDefault)
            }).ToList()
        };
    }

    public async Task<AdminBannerFormViewModel> GetFormAsync(int? id)
    {
        var model = id.HasValue
            ? await _context.Banners
                .AsNoTracking()
                .Where(x => x.BannerId == id.Value && !x.IsDeleted)
                .Select(x => new AdminBannerFormViewModel
                {
                    BannerId = x.BannerId,
                    Name = x.Name,
                    DesktopImageUrl = x.DesktopImageUrl,
                    MobileImageUrl = x.MobileImageUrl,
                    AltText = x.AltText,
                    Notes = x.Notes,
                    IsActive = x.IsActive,
                    TargetType = x.BannerTarget != null ? x.BannerTarget.TargetType : "url",
                    ExternalUrl = x.BannerTarget != null ? x.BannerTarget.ExternalUrl : null,
                    TargetCategoryId = x.BannerTarget != null ? x.BannerTarget.CategoryId : null,
                    TargetBrandId = x.BannerTarget != null ? x.BannerTarget.BrandId : null,
                    TargetProductId = x.BannerTarget != null ? x.BannerTarget.ProductId : null,
                    OpenInNewTab = x.BannerTarget != null && x.BannerTarget.OpenInNewTab,
                    PositionMaps = x.BannerPositionMaps
                        .OrderByDescending(map => map.Priority)
                        .Select(map => new BannerPositionMapInputViewModel
                        {
                            BannerPositionMapId = map.BannerPositionMapId,
                            BannerPositionId = map.BannerPositionId,
                            DisplayCategoryId = map.DisplayCategoryId,
                            DisplayBrandId = map.DisplayBrandId,
                            ScopeType = ResolveScopeType(map.DisplayCategoryId, map.DisplayBrandId),
                            Priority = map.Priority,
                            StartAt = map.StartAt,
                            EndAt = map.EndAt,
                            IsDefault = map.IsDefault,
                            IsActive = map.IsActive
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync()
            : new AdminBannerFormViewModel
            {
                PositionMaps = new List<BannerPositionMapInputViewModel>
                {
                    new() { Priority = 100, IsActive = true }
                }
            };

        if (model == null)
        {
            return new AdminBannerFormViewModel();
        }

        model.Categories = await GetCategoryOptionsAsync();
        model.Brands = await GetBrandOptionsAsync();
        model.Positions = await GetPositionOptionsAsync();

        if (model.TargetProductId.HasValue)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductId == model.TargetProductId.Value)
                .Select(x => new BannerLookupOptionViewModel
                {
                    Id = x.ProductId,
                    Label = $"{x.Name} ({x.Sku ?? $"ID {x.ProductId}"})"
                })
                .FirstOrDefaultAsync();

            if (product != null)
            {
                model.InitialProducts = new List<BannerLookupOptionViewModel> { product };
            }
        }

        return model;
    }

    public async Task SaveAsync(AdminBannerFormViewModel model)
    {
        NormalizePositionMaps(model);
        ValidateModel(model);

        Banner banner;
        if (model.BannerId > 0)
        {
            banner = await _context.Banners
                .Include(x => x.BannerTarget)
                .Include(x => x.BannerPositionMaps)
                .FirstOrDefaultAsync(x => x.BannerId == model.BannerId && !x.IsDeleted)
                ?? throw new InvalidOperationException("Không tìm thấy banner.");
        }
        else
        {
            banner = new Banner
            {
                CreatedAt = DateTime.Now
            };
            await _context.Banners.AddAsync(banner);
        }

        banner.Name = model.Name.Trim();
        banner.AltText = string.IsNullOrWhiteSpace(model.AltText) ? model.Name.Trim() : model.AltText.Trim();
        banner.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        banner.IsActive = model.IsActive;
        banner.IsDeleted = false;
        banner.UpdatedAt = DateTime.Now;
        banner.DesktopImageUrl = await ResolveImagePathAsync(model.DesktopImageFile, model.DesktopImageUrl, "desktop", banner.DesktopImageUrl);
        banner.MobileImageUrl = await ResolveImagePathAsync(model.MobileImageFile, model.MobileImageUrl, "mobile", banner.MobileImageUrl);

        if (string.IsNullOrWhiteSpace(banner.DesktopImageUrl))
        {
            throw new InvalidOperationException("Vui lòng chọn ảnh banner.");
        }

        if (banner.BannerTarget == null)
        {
            banner.BannerTarget = new BannerTarget
            {
                CreatedAt = DateTime.Now
            };
        }

        banner.BannerTarget.TargetType = model.TargetType.Trim().ToLowerInvariant();
        banner.BannerTarget.ExternalUrl = string.IsNullOrWhiteSpace(model.ExternalUrl) ? null : model.ExternalUrl.Trim();
        banner.BannerTarget.CategoryId = model.TargetCategoryId;
        banner.BannerTarget.BrandId = model.TargetBrandId;
        banner.BannerTarget.ProductId = model.TargetProductId;
        banner.BannerTarget.OpenInNewTab = model.OpenInNewTab;
        banner.BannerTarget.UpdatedAt = DateTime.Now;

        await ValidateTargetReferencesAsync(banner.BannerTarget);

        _context.BannerPositionMaps.RemoveRange(banner.BannerPositionMaps);
        banner.BannerPositionMaps.Clear();

        foreach (var map in model.PositionMaps)
        {
            banner.BannerPositionMaps.Add(new BannerPositionMap
            {
                BannerPositionId = map.BannerPositionId,
                DisplayCategoryId = map.DisplayCategoryId,
                DisplayBrandId = map.DisplayBrandId,
                Priority = map.Priority,
                StartAt = map.StartAt,
                EndAt = map.EndAt,
                IsDefault = map.IsDefault,
                IsActive = map.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        await EnsureSingleDefaultPerScopeAsync(banner.BannerId, model.PositionMaps);

        await _context.SaveChangesAsync();
        await InvalidateCacheAsync();
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var banner = await _context.Banners
            .Include(x => x.BannerPositionMaps)
            .FirstOrDefaultAsync(x => x.BannerId == id && !x.IsDeleted);

        if (banner == null)
        {
            return (false, "Không tìm thấy banner.");
        }

        banner.IsDeleted = true;
        banner.IsActive = false;
        banner.UpdatedAt = DateTime.Now;

        foreach (var map in banner.BannerPositionMaps)
        {
            map.IsActive = false;
            map.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        await InvalidateCacheAsync();
        return (true, "Đã ẩn banner khỏi hệ thống.");
    }

    public async Task<IReadOnlyList<BannerLookupOptionViewModel>> SearchProductsAsync(string? keyword)
    {
        var query = _context.Products.AsNoTracking().Where(x => x.Visible == true);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalizedKeyword) ||
                (x.Sku != null && x.Sku.Contains(normalizedKeyword)));
        }

        return await query
            .OrderBy(x => x.Name)
            .Take(20)
            .Select(x => new BannerLookupOptionViewModel
            {
                Id = x.ProductId,
                Label = $"{x.Name} ({x.Sku ?? $"ID {x.ProductId}"})"
            })
            .ToListAsync();
    }

    public Task InvalidateCacheAsync()
    {
        var currentVersion = _memoryCache.Get<int?>(CacheVersionKey) ?? 1;
        _memoryCache.Set(CacheVersionKey, currentVersion + 1, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    private async Task EnsureSingleDefaultPerScopeAsync(int currentBannerId, IEnumerable<BannerPositionMapInputViewModel> submittedMaps)
    {
        var defaultScopes = submittedMaps
            .Where(x => x.IsDefault)
            .Select(x => new { x.BannerPositionId, x.DisplayCategoryId, x.DisplayBrandId })
            .Distinct()
            .ToList();

        foreach (var scope in defaultScopes)
        {
            var oldDefaults = await _context.BannerPositionMaps
                .Where(x =>
                    x.BannerId != currentBannerId &&
                    x.BannerPositionId == scope.BannerPositionId &&
                    x.DisplayCategoryId == scope.DisplayCategoryId &&
                    x.DisplayBrandId == scope.DisplayBrandId &&
                    x.IsDefault)
                .ToListAsync();

            foreach (var oldDefault in oldDefaults)
            {
                oldDefault.IsDefault = false;
                oldDefault.UpdatedAt = DateTime.Now;
            }
        }
    }

    private static void NormalizePositionMaps(AdminBannerFormViewModel model)
    {
        model.PositionMaps = model.PositionMaps
            .Where(x => x.BannerPositionId > 0)
            .ToList();

        foreach (var map in model.PositionMaps)
        {
            switch (map.ScopeType)
            {
                case BannerScopeType.Category:
                    map.DisplayBrandId = null;
                    break;
                case BannerScopeType.Brand:
                    map.DisplayCategoryId = null;
                    break;
                default:
                    map.DisplayCategoryId = null;
                    map.DisplayBrandId = null;
                    break;
            }
        }
    }

    private static void ValidateModel(AdminBannerFormViewModel model)
    {
        if (model.PositionMaps.Count == 0)
        {
            throw new InvalidOperationException("Vui lòng cấu hình ít nhất một vị trí hiển thị cho banner.");
        }

        foreach (var map in model.PositionMaps)
        {
            switch (map.ScopeType)
            {
                case BannerScopeType.Category when !map.DisplayCategoryId.HasValue:
                    throw new InvalidOperationException("Vui lòng chọn danh mục áp dụng cho vị trí theo danh mục.");
                case BannerScopeType.Brand when !map.DisplayBrandId.HasValue:
                    throw new InvalidOperationException("Vui lòng chọn thương hiệu áp dụng cho vị trí theo thương hiệu.");
            }

            if (map.StartAt.HasValue && map.EndAt.HasValue && map.StartAt.Value > map.EndAt.Value)
            {
                throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
            }
        }

        var duplicateDefaults = model.PositionMaps
            .Where(x => x.IsDefault)
            .GroupBy(x => new { x.BannerPositionId, x.DisplayCategoryId, x.DisplayBrandId })
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateDefaults != null)
        {
            throw new InvalidOperationException("Mỗi vị trí và phạm vi chỉ được có một banner fallback.");
        }

        switch (model.TargetType?.Trim().ToLowerInvariant())
        {
            case "url":
                if (string.IsNullOrWhiteSpace(model.ExternalUrl))
                {
                    throw new InvalidOperationException("Vui lòng nhập liên kết đích.");
                }
                break;
            case "category":
                if (!model.TargetCategoryId.HasValue)
                {
                    throw new InvalidOperationException("Vui lòng chọn danh mục đích.");
                }
                break;
            case "brand":
                if (!model.TargetBrandId.HasValue)
                {
                    throw new InvalidOperationException("Vui lòng chọn thương hiệu đích.");
                }
                break;
            case "product":
                if (!model.TargetProductId.HasValue)
                {
                    throw new InvalidOperationException("Vui lòng chọn sản phẩm đích.");
                }
                break;
            default:
                throw new InvalidOperationException("Loại điều hướng banner không hợp lệ.");
        }
    }

    private async Task ValidateTargetReferencesAsync(BannerTarget target)
    {
        if (target.CategoryId.HasValue && !await _context.Categories.AnyAsync(x => x.CategoryId == target.CategoryId.Value))
        {
            throw new InvalidOperationException("Danh mục đích không tồn tại.");
        }

        if (target.BrandId.HasValue && !await _context.Brands.AnyAsync(x => x.BrandId == target.BrandId.Value))
        {
            throw new InvalidOperationException("Thương hiệu đích không tồn tại.");
        }

        if (target.ProductId.HasValue && !await _context.Products.AnyAsync(x => x.ProductId == target.ProductId.Value))
        {
            throw new InvalidOperationException("Sản phẩm đích không tồn tại.");
        }
    }

    private async Task<string> ResolveImagePathAsync(IFormFile? file, string? currentInput, string suffix, string? existingValue)
    {
        if (file == null || file.Length == 0)
        {
            return string.IsNullOrWhiteSpace(currentInput) ? existingValue ?? string.Empty : currentInput.Trim();
        }

        var extension = Path.GetExtension(file.FileName);
        var safeFileName = $"{Guid.NewGuid():N}-{suffix}{extension}";
        var uploadDirectory = Path.Combine(_environment.WebRootPath, "Upload", "Banner");
        Directory.CreateDirectory(uploadDirectory);
        var absolutePath = Path.Combine(uploadDirectory, safeFileName);

        await using var stream = new FileStream(absolutePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/Upload/Banner/{safeFileName}";
    }

    private async Task<IReadOnlyList<BannerPositionOptionViewModel>> GetPositionOptionsAsync()
    {
        return await _context.BannerPositions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new BannerPositionOptionViewModel
            {
                BannerPositionId = x.BannerPositionId,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<BannerLookupOptionViewModel>> GetCategoryOptionsAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BannerLookupOptionViewModel
            {
                Id = x.CategoryId,
                Label = x.Name
            })
            .ToListAsync();
    }

    private async Task<IReadOnlyList<BannerLookupOptionViewModel>> GetBrandOptionsAsync()
    {
        return await _context.Brands
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new BannerLookupOptionViewModel
            {
                Id = x.BrandId,
                Label = x.Name
            })
            .ToListAsync();
    }

    private static string BuildTargetSummary(BannerTarget? target)
    {
        if (target == null)
        {
            return "-";
        }

        return target.TargetType switch
        {
            "category" => $"Danh mục: {target.Category?.Name ?? "Không xác định"}",
            "brand" => $"Thương hiệu: {target.Brand?.Name ?? "Không xác định"}",
            "product" => $"Sản phẩm: {target.Product?.Name ?? "Không xác định"}",
            _ => $"URL: {target.ExternalUrl ?? "/"}"
        };
    }

    private static string BuildPositionSummary(IEnumerable<BannerPositionMap> maps)
    {
        var values = maps
            .OrderByDescending(x => x.Priority)
            .Select(x =>
            {
                var baseLabel = x.BannerPosition.Name;

                if (x.DisplayBrand != null)
                {
                    baseLabel += $" / TH: {x.DisplayBrand.Name}";
                }
                else if (x.DisplayCategory != null)
                {
                    baseLabel += $" / DM: {x.DisplayCategory.Name}";
                }
                else
                {
                    baseLabel += " / Tất cả";
                }

                if (x.IsDefault)
                {
                    baseLabel += " (fallback)";
                }

                return baseLabel;
            })
            .ToList();

        return values.Count == 0 ? "-" : string.Join(" | ", values);
    }

    private static string BuildScheduleSummary(IEnumerable<BannerPositionMap> maps)
    {
        var activeRange = maps
            .OrderByDescending(x => x.Priority)
            .Select(x =>
            {
                var from = x.StartAt?.ToString("dd/MM/yyyy HH:mm") ?? "Ngay";
                var to = x.EndAt?.ToString("dd/MM/yyyy HH:mm") ?? "không giới hạn";
                return $"{from} - {to}";
            })
            .FirstOrDefault();

        return activeRange ?? "-";
    }

    private static string BuildQueryString(string? keyword, int? positionId, string? targetType, string? status)
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            values.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (positionId.HasValue)
        {
            values.Add($"positionId={positionId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            values.Add($"targetType={Uri.EscapeDataString(targetType)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            values.Add($"status={Uri.EscapeDataString(status)}");
        }

        return string.Join("&", values);
    }

    private static BannerScopeType ResolveScopeType(int? displayCategoryId, int? displayBrandId)
    {
        if (displayBrandId.HasValue)
        {
            return BannerScopeType.Brand;
        }

        if (displayCategoryId.HasValue)
        {
            return BannerScopeType.Category;
        }

        return BannerScopeType.All;
    }
}
