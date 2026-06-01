using Tech_Store.Models;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Admin.Interfaces;

public interface IStockManagerService
{
    Task<AdminStockTransactionFormViewModel?> GetEditTransactionFormAsync(int inventoryTransactionId);

    Task PrepareTransactionFormAsync(AdminStockTransactionFormViewModel model);

    Task CreateTransactionAsync(AdminStockBatchImportFormViewModel model, int userId);

    Task UpdateTransactionAsync(int inventoryTransactionId, AdminStockTransactionFormViewModel model, int userId);

    Task DeleteTransactionAsync(int inventoryTransactionId);

    Task<AdminStockBatchImportFormViewModel> GetBatchTransactionFormAsync(int[] selectedProductIds);

    Task PrepareBatchTransactionFormAsync(AdminStockBatchImportFormViewModel model);

    Task<IReadOnlyList<AdminStockBatchImportProductViewModel>> GetBatchTransactionProductsAsync(int[] selectedProductIds);

    Task<AdminStockHistoryIndexViewModel> GetHistoryAsync(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page, int pageSize);

    Task<InventoryTransactionsVM?> GetHistoryDetailAsync(int id);
}
