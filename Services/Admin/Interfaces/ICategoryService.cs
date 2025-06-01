using Tech_Store.Models;

namespace Tech_Store.Services.Admin.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task CreateCategoryAsync(Category category, IFormFile? imageFile);
        Task<bool> UpdateCategoryAsync(int id, Category updatedCategory, IFormFile? imageFile);
        Task<bool> DeleteCategoryAsync(int id);
        // Đổi trạng thái hiển thị của danh mục
        Task<bool> ChangeVisible(int id);
    }
}
