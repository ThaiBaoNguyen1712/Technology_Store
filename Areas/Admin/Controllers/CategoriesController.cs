using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var list_cate = _context.Categories.ToList();
            return View(list_cate);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create(Category cate)
        {
            _context.Categories.Add(cate);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var cate = _context.Categories.FirstOrDefault(x=>x.CategoryId==id);
            if(cate ==null)
            {
                return NotFound();
            }
            return Json(cate);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category cate)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
                if (category == null)
                {
                    return NotFound();
                }

                category.Name = cate.Name;
                category.Description = cate.Description;
                category.Visible = cate.Visible;
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
                var cate = await _context.Categories.FindAsync(id); // Tìm danh mục theo ID
                if (cate == null)
                {
                    return BadRequest();
                }

                _context.Categories.Remove(cate);
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
