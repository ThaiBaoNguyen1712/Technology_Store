using Microsoft.EntityFrameworkCore;
using System.IO;
using Tech_Store.Models;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.CategoryServices
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo");
        // Đường dẫn lưu trữ hình ảnh
        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }
        // Truy vấn tất cả danh mục
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.OrderByDescending(x=>x.CategoryId).ToListAsync();
        }
        // Truy vấn danh mục theo ID
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }
        // Tạo danh mục mới
        public async Task CreateCategoryAsync(Category category, IFormFile? imageFile)
        {
            category.Image = await SaveImageAsync(imageFile) ?? "none.jpg";
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
        // Cập nhật thông tin danh mục
        public async Task<bool> UpdateCategoryAsync(int id, Category updatedCategory, IFormFile? imageFile)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return false;

            // Nếu có hình ảnh mới, xóa hình ảnh cũ và lưu hình ảnh mới
            if (imageFile != null && imageFile.Length > 0)
            {
                DeleteImage(category.Image);
                category.Image = await SaveImageAsync(imageFile);
            }

            category.Name = updatedCategory.Name;
            category.Description = updatedCategory.Description;
            category.EngTitle = updatedCategory.EngTitle;
            category.Visible = updatedCategory.Visible; 

            await _context.SaveChangesAsync();
            return true;
        }
        // Xóa danh mục theo ID
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            DeleteImage(category.Image);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
        // Đổi trạng thái hiển thị của danh mục
        public async Task<bool> ChangeVisible(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return false;

            category.Visible = category.Visible == 0 ? 1 : 0;
            await _context.SaveChangesAsync();
            return true;
        }
        // Lưu hình ảnh lên server
        private async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var fileName = $"Cate_{Guid.NewGuid()}.png";
            var imagePath = Path.Combine(_uploadPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
            using var stream = new FileStream(imagePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return fileName;
        }
        // Xóa hình ảnh khỏi server
        private void DeleteImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == "none.jpg") return;

            var path = Path.Combine(_uploadPath, fileName);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
