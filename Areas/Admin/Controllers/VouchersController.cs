using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class VouchersController : BaseAdminController
    {
        private readonly IVoucherService _voucherService;
        public VouchersController(ApplicationDbContext context, IVoucherService voucherService) : base(context)
        {
            _voucherService= voucherService;
        }

        // GET: Admin/Vouchers
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Vouchers.OrderByDescending(x=>x.VoucherId).ToListAsync());
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Voucher vou)
        {
            if (vou == null)
            {
                   return BadRequest("Voucher không được để trống.");
            }
            _voucherService.CreateVoucherAsync(vou).Wait();
            return Ok();
        }

        [Route("Edit")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
                return NotFound();

            var voucher = await _voucherService.GetVoucherByIdAsync(id);
            return Json(voucher);
        }
        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody] Voucher vou)
        {
            if (vou == null)
            {
                return BadRequest("Voucher không được để trống.");
            }
            var result = await _voucherService.UpdateVoucherAsync(vou.VoucherId, vou);
            if (result)
            {
                return Ok();
            }
            else
            {
                return NotFound("Không tìm thấy voucher với ID đã cho.");
            }
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
           var result = await _voucherService.DeleteVoucherAsync(id);
            if (result)
            {
                return Ok();
            }
            else
            {
                return NotFound("Không tìm thấy voucher với ID đã cho.");
            }
        }
        [HttpGet]
        [Route("Filter")]
        public IActionResult Filter(string? code, string? name, DateTime? dateFrom, DateTime? dateTo)
        {
            var voucher = _context.Vouchers.AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (!string.IsNullOrEmpty(code))
            {
                voucher = voucher.Where(v => v.Code == code);
            }
            if (!string.IsNullOrEmpty(name))
            {
                voucher = voucher.Where(v => v.Name != null && v.Name.Contains(name));
            }
          
            if (dateFrom.HasValue)
            {
                voucher = voucher.Where(v => v.StartedAt >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                voucher = voucher.Where(v => v.ExpiredAt <= dateTo.Value);
            }
            // Chuyển đổi sang view model để trả về JSON
            var result = voucher.OrderByDescending(p => p.VoucherId).Take(100).ToList();

            return Json(result);
        }
    }
}
