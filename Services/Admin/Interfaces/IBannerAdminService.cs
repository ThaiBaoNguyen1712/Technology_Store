using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Admin.Interfaces;

public interface IBannerAdminService
{
    Task<AdminBannerIndexViewModel> GetIndexAsync(string? keyword, int? positionId, string? targetType, string? status, int page, int pageSize);

    Task<AdminBannerFormViewModel> GetFormAsync(int? id);

    Task SaveAsync(AdminBannerFormViewModel model);

    Task<(bool Success, string Message)> DeleteAsync(int id);

    Task<IReadOnlyList<BannerLookupOptionViewModel>> SearchProductsAsync(string? keyword);

    Task InvalidateCacheAsync();
}
