using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Admin.Interfaces
{
    public interface IBrandService
    {
        Task<AdminBrandIndexViewModel> GetAdminBrandIndexAsync(string? keyword, int? categoryId, int page, int pageSize);
        Task<AdminBrandFormViewModel?> GetBrandFormAsync(int? id);
        Task SaveBrandAsync(AdminBrandFormViewModel model, IFormFile? imageFile);
        Task<bool> DeleteBrandAsync(int id);
    }
}
