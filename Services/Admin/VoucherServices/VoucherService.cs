using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.VoucherServices
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;
        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }
        // Truy vấn tất cả voucher
        public async Task<List<Voucher>> GetAllVouchersAsync()
        {
            return await _context.Vouchers.OrderByDescending(x => x.VoucherId).ToListAsync();
        }
        // Truy vấn voucher theo ID
        public async Task<Voucher?> GetVoucherByIdAsync(int id)
        {
            return await _context.Vouchers.FindAsync(id);
        }
        // Tạo voucher mới
        public async Task CreateVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
        }
        // Cập nhật thông tin voucher
            public async Task<bool> UpdateVoucherAsync(int id, Voucher updatedVoucher)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == id);
            if (voucher == null) return false;

            voucher.Name = updatedVoucher.Name;
            voucher.Code = updatedVoucher.Code;
            voucher.Quantity = updatedVoucher.Quantity;
            voucher.StartedAt = updatedVoucher.StartedAt;
            voucher.ExpiredAt = updatedVoucher.ExpiredAt;
            voucher.Promotion = updatedVoucher.Promotion;

            await _context.SaveChangesAsync();
            return true;
        }
        // Xóa voucher theo ID
        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return false;

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return true;
        }
        // Kiểm tra xem voucher có tồn tại hay không
        public async Task<bool> VoucherExistsAsync(int id)
        {
            return await _context.Vouchers.AnyAsync(v => v.VoucherId == id);
        }
        // Kiểm tra xem mã voucher có tồn tại hay không
        public async Task<bool> VoucherCodeExistsAsync(string code)
        {
            return await _context.Vouchers.AnyAsync(v => v.Code == code);
        }
        // Kiểm tra xem mã voucher có tồn tại và không phải là mã của voucher hiện tại
        public async Task<bool> VoucherCodeExistsAsync(string code, int id)
        {
            return await _context.Vouchers.AnyAsync(v => v.Code == code && v.VoucherId != id);
        }
        // Kiểm tra xem voucher có hợp lệ hay không
        public async Task<bool> IsVoucherValidAsync(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null) return false;

            var currentDate = DateTime.Now;
            return voucher.StartedAt <= currentDate && voucher.ExpiredAt >= currentDate;
        }
        // Lấy thông tin voucher theo mã
        public async Task<Voucher?> GetVoucherByCodeAsync(string code)
        {
            return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        }
        // Lấy danh sách voucher theo mã
        public async Task<List<Voucher>> GetVouchersByCodeAsync(string code)
        {
            return await _context.Vouchers
                .Where(v => v.Code.Contains(code))
                .OrderByDescending(v => v.VoucherId)
                .ToListAsync();
        }

    }
}
