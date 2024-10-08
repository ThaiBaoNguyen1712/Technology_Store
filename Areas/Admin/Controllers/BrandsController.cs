using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var list_brands = _context.Brands.ToList();
            return View(list_brands);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = _context.Brands.FirstOrDefault(x=>x.BrandId==id);
            if(brand == null)
            {
                return NotFound();
            }
            return Json(brand);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Brand _brand)
        {
            try
            {
                var brand = await _context.Brands.FirstOrDefaultAsync(x => x.BrandId == id);
                if (_brand == null)
                {
                    return NotFound();
                }

                brand.Name = _brand.Name;
                brand.Description = _brand.Description;
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                // Ghi lại thông tin lỗi
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id); // Tìm danh mục theo ID
                if (brand == null)
                {
                    return BadRequest();
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
