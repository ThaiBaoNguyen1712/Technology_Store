using Tech_Store.Models;

namespace Tech_Store.Services.Admin.Interfaces
{
    public interface IVoucherService
    {
        Task<List<Voucher>> GetAllVouchersAsync();
        Task<Voucher?> GetVoucherByIdAsync(int id);
        Task CreateVoucherAsync(Voucher voucher);
        Task<bool> UpdateVoucherAsync(int id, Voucher updatedVoucher);
        Task<bool> DeleteVoucherAsync(int id);
        
    }
}
