using Tech_Store.Models;

namespace Tech_Store.Services.Admin.Interfaces
{
    public interface IBrandService
    {
        Task<List<Brand>> GetAllBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task CreateBrandAsync(Brand brand, IFormFile? imageFile);
        Task<bool> UpdateBrandAsync(int id, Brand updatedBrand, IFormFile? imageFile);
        Task<bool> DeleteBrandAsync(int id);
    }
}
