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
    public class VouchersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VouchersController(ApplicationDbContext context)
        {
            _context = context;
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

        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
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
        [HttpPut("Update/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Voucher vou)
        {
            if (id == null)
                return NotFound();
            if (ModelState.IsValid)
            {
                var voucher = await _context.Vouchers.FindAsync(id);
                if (voucher == null)
                {
                    return NotFound();
                }
                voucher.Name = vou.Name;
                voucher.Description = vou.Description;
                voucher.Code = vou.Code;
                voucher.Promotion = vou.Promotion;
                voucher.ExpiredAt = vou.ExpiredAt;
                voucher.StartedAt = vou.StartedAt;
                await _context.SaveChangesAsync();
                return Ok();
            }
            else
                return BadRequest();
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
