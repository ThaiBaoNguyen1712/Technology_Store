using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.BrandServices
{
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo");

        public BrandService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminBrandIndexViewModel> GetAdminBrandIndexAsync(string? keyword, int? categoryId, int page, int pageSize)
        {
            var query = _context.Brands
                .AsNoTracking()
                .Include(b => b.BrandCategories)
                    .ThenInclude(bc => bc.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(b =>
                    b.Name.Contains(normalizedKeyword) ||
                    (b.Description != null && b.Description.Contains(normalizedKeyword)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.BrandCategories.Any(bc => bc.CategoryId == categoryId.Value));
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            var brands = await query
                .OrderBy(b => b.SortOrder ?? 0)
                .ThenBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new AdminBrandIndexItemViewModel
                {
                    BrandId = b.BrandId,
                    Name = b.Name,
                    Description = b.Description,
                    Image = b.Image,
                    SortOrder = b.SortOrder ?? 0,
                    UpdatedAt = b.UpdatedAt,
                    CategoryNames = b.BrandCategories
                        .OrderBy(bc => bc.Category.Name)
                        .Select(bc => bc.Category.Name)
                        .ToList()
                })
                .ToListAsync();

            return new AdminBrandIndexViewModel
            {
                Keyword = keyword,
                CategoryId = categoryId,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                Brands = brands
            };
        }

        public async Task<AdminBrandFormViewModel?> GetBrandFormAsync(int? id)
        {
            if (!id.HasValue)
            {
                return new AdminBrandFormViewModel();
            }

            return await _context.Brands
                .AsNoTracking()
                .Include(b => b.BrandCategories)
                .Where(b => b.BrandId == id.Value)
                .Select(b => new AdminBrandFormViewModel
                {
                    BrandId = b.BrandId,
                    Name = b.Name,
                    Description = b.Description,
                    ExistingImage = b.Image,
                    SortOrder = b.SortOrder ?? 0,
                    CategoryIds = b.BrandCategories
                        .OrderBy(bc => bc.CategoryId)
                        .Select(bc => bc.CategoryId)
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task SaveBrandAsync(AdminBrandFormViewModel model, IFormFile? imageFile)
        {
            var selectedCategoryIds = (model.CategoryIds ?? new List<int>())
                .Distinct()
                .ToList();

            Brand brand;
            if (model.BrandId > 0)
            {
                brand = await _context.Brands
                    .Include(b => b.BrandCategories)
                    .FirstOrDefaultAsync(b => b.BrandId == model.BrandId)
                    ?? throw new InvalidOperationException("Không tìm thấy thương hiệu.");
            }
            else
            {
                brand = new Brand();
                _context.Brands.Add(brand);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                DeleteImage(brand.Image);
                brand.Image = await SaveImageAsync(imageFile);
            }
            else if (brand.BrandId == 0)
            {
                brand.Image = "none.jpg";
            }

            brand.Name = model.Name.Trim();
            brand.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            brand.SortOrder = model.SortOrder;
            brand.CategoryId = selectedCategoryIds.Count > 0 ? selectedCategoryIds[0] : null;

            var currentMappings = brand.BrandCategories.ToDictionary(x => x.CategoryId, x => x);

            foreach (var removed in brand.BrandCategories.Where(bc => !selectedCategoryIds.Contains(bc.CategoryId)).ToList())
            {
                _context.BrandCategories.Remove(removed);
            }

            foreach (var categoryId in selectedCategoryIds)
            {
                if (currentMappings.ContainsKey(categoryId))
                {
                    continue;
                }

                brand.BrandCategories.Add(new BrandCategory
                {
                    Brand = brand,
                    CategoryId = categoryId
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.BrandCategories)
                .FirstOrDefaultAsync(b => b.BrandId == id);
            if (brand == null)
            {
                return false;
            }

            DeleteImage(brand.Image);
            if (brand.BrandCategories.Count > 0)
            {
                _context.BrandCategories.RemoveRange(brand.BrandCategories);
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            Directory.CreateDirectory(_uploadPath);
            var fileName = $"Brand_{Guid.NewGuid()}.png";
            var path = Path.Combine(_uploadPath, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeleteImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == "none.jpg")
            {
                return;
            }

            var path = Path.Combine(_uploadPath, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
