using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class BrandsController : BaseAdminController
    {
        private readonly IBrandService _brandService;

        public BrandsController(ApplicationDbContext context, IBrandService brandService) : base(context)
        {
            _brandService = brandService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.cate = _context.Categories.ToList();
            var brands = await _brandService.GetAllBrandsAsync();
            return View(brands);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm]Brand brand,IFormFile? imageFile)
        {   
            if (brand == null)
            {
                return BadRequest("Dữ liệu brand không được trống");
            }
            _brandService.CreateBrandAsync(brand, imageFile).Wait();
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = _brandService.GetBrandByIdAsync(id).Result;
            if(brand == null)
            {
                return NotFound();
            }
            return Json(brand);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Brand _brand,IFormFile? imageFile)
        {
           var result = await _brandService.UpdateBrandAsync(id, _brand, imageFile);
            if (result)
            {
                return Ok();
            }
            else
            {
                return NotFound("Không tìm thấy brand với ID đã cho");
            }
        }
        // Xóa brand theo ID
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
          var result = await _brandService.DeleteBrandAsync(id);
            if (result)
            {
                return Ok();
            }
            else
            {
                return NotFound("Không tìm thấy brand với ID đã cho");
            }

        }
    }
}
