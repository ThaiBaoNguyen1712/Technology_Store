using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Admin.Interfaces;

public interface ISupplierService
{
    Task<AdminSupplierIndexViewModel> GetAdminSupplierIndexAsync(string? code, string? name, string? phoneNumber, string? status, int page, int pageSize);

    Task<AdminSupplierFormViewModel?> GetSupplierFormAsync(int? id);

    Task SaveSupplierAsync(AdminSupplierFormViewModel model);

    Task<(bool Success, string Message)> DeleteSupplierAsync(int id);
}
