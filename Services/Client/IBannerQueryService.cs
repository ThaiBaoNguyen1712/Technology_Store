using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Client;

public interface IBannerQueryService
{
    Task<IReadOnlyList<BannerRenderItemViewModel>> GetBannersAsync(string positionCode, int? categoryId = null, int? brandId = null, int take = 1);

    Task<string> ResolveNavigationUrlAsync(int bannerId);

    Task<bool> ShouldOpenInNewTabAsync(int bannerId);
}
