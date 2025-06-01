using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.BrandServices
{
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _context;
        // Đường dẫn lưu trữ hình ảnh
        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo");
        public BrandService(ApplicationDbContext context)
        {
            _context = context;
        }
        // Truy vấn tất cả các brand
        public async Task<List<Brand>> GetAllBrandsAsync()
        {
            return await _context.Brands.Include(b => b.Category).ToListAsync();
        }
        //Truy vấn brand theo ID
        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            return await _context.Brands.FindAsync(id);
        }
        // Tạo brand mới
        public async Task CreateBrandAsync(Brand brand, IFormFile? imageFile)
        {
            brand.Image = await SaveImageAsync(imageFile) ?? "none.jpg";
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
        }
        // Cập nhật thông tin brand
        public async Task<bool> UpdateBrandAsync(int id, Brand updatedBrand, IFormFile? imageFile)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandId == id);
            if (brand == null) return false;

            if (imageFile != null && imageFile.Length > 0)
            {
                DeleteImage(brand.Image);
                brand.Image = await SaveImageAsync(imageFile);
            }

            brand.Name = updatedBrand.Name;
            brand.Description = updatedBrand.Description;
            brand.CategoryId = updatedBrand.CategoryId;
            await _context.SaveChangesAsync();
            return true;
        }
        // Xóa brand theo ID
        public async Task<bool> DeleteBrandAsync(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return false;

            DeleteImage(brand.Image);
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return true;
        }
        // Save hình ảnh lên server
        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            Directory.CreateDirectory(_uploadPath);
            var fileName = $"Brand_{Guid.NewGuid()}.png";
            var path = Path.Combine(_uploadPath, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

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
