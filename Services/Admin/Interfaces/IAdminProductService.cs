using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Admin.Interfaces
{
    public interface IAdminProductService
    {
        Task<AdminProductDetailData> GetDetailAsync(int id);
        Task<AdminProductIndexData> GetIndexDataAsync(AdminProductFilterRequest request);
        Task<List<ProductViewModel>> FilterAsync(AdminProductFilterRequest request);
        Task<AdminProductLookupData> GetLookupDataAsync();
        Task<int> GetNextSortOrderAsync();
        Task<AdminProductEditData?> GetEditDataAsync(int id);
        Task<AdminProductActionResult> CreateAsync(ProductDTo productDto);
        Task<AdminProductActionResult> UpdateAsync(ProductDTo productDto);
        Task<AdminProductActionResult> ChangeVisibleAsync(int productId);
        Task<AdminProductActionResult> DeleteAsync(int id);
        Task<AdminProductActionResult> DeleteMainImageAsync(DeleteFileViewModel model);
        Task<AdminProductActionResult> DeleteGalleryImageAsync(DeleteFileViewModel model);
        Task<AdminProductCodeData?> GetCodeDataAsync(int id, string? content, string? codeType, string scheme);
        Task<AdminProductActionResult> BuildPrintCodeAsync(int id, string content, string codeType, int quantity);
    }
}
