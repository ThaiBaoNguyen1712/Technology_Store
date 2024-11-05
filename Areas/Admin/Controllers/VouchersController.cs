using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class VouchersController : BaseAdminController
    {

        public VouchersController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Admin/Vouchers
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Vouchers.ToListAsync());
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Voucher vou)
        {
            if (ModelState.IsValid)
            {
                _context.Vouchers.Add(vou);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }

        [Route("Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
                return NotFound();

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher ==null)
            {
                return BadRequest();
            }
            return Json(voucher);
        }
        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody] Voucher vou)
        {
            if (ModelState.IsValid)
            {
                var voucher = await _context.Vouchers.FindAsync(vou.VoucherId);
                if (voucher == null)
                {
                    return NotFound();
                }
                voucher.Name = vou.Name;
                voucher.Description = vou.Description;
                voucher.Code = vou.Code.Trim();
                voucher.Promotion = vou.Promotion;
                voucher.Quantity = vou.Quantity;
                voucher.ExpiredAt = vou.ExpiredAt;
                voucher.StartedAt = vou.StartedAt;
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorMessage); // hoặc dùng log để kiểm tra
                }
                return BadRequest(ModelState); // có thể trả về chi tiết lỗi của model
            }

        }

        [HttpDelete("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var vou = await _context.Vouchers.FindAsync(id); // Tìm danh mục theo ID
                if (vou == null)
                {
                    return BadRequest();
                }

                _context.Vouchers.Remove(vou);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
