using Tech_Store.Models;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Client.Storefront
{
    public interface IHomePageContentService
    {
        Task<HomePageContentResult> GetHomePageContentAsync();
        Task<CategoryLandingContentResult> GetCategoryLandingContentAsync();
    }

    public sealed class HomePageContentResult
    {
        public List<Category> Categories { get; init; } = new();
        public Dictionary<string, List<string>> TopHotSearchByCategory { get; init; } = new();
        public Dictionary<string, List<string>> HotSearchByCategory { get; init; } = new();
        public Dictionary<int, List<Brand>> BrandsByCategory { get; init; } = new();
        public List<HomeCategoryProductsSection> ProductsByCategory { get; init; } = new();
        public List<Product> HotSaleProducts { get; init; } = new();
        public IReadOnlyList<BannerRenderItemViewModel> HomeHeroMainBanners { get; init; } = Array.Empty<BannerRenderItemViewModel>();
        public IReadOnlyList<BannerRenderItemViewModel> HomeHeroPromoBanners { get; init; } = Array.Empty<BannerRenderItemViewModel>();
    }

    public sealed class HomeCategoryProductsSection
    {
        public Category Category { get; init; } = null!;
        public List<Product> Products { get; init; } = new();
    }

    public sealed class CategoryLandingContentResult
    {
        public List<Category> CategoryCards { get; init; } = new();
        public Dictionary<int, int> CategoryProductCounts { get; init; } = new();
        public List<Product> FeaturedProducts { get; init; } = new();
    }
}
