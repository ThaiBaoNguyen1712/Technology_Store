using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.SupplierServices;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _context;

    public SupplierService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminSupplierIndexViewModel> GetAdminSupplierIndexAsync(string? code, string? name, string? phoneNumber, string? status, int page, int pageSize)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Include(x => x.InventoryTransactions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(code))
        {
            var normalizedCode = code.Trim();
            query = query.Where(x => x.Code.Contains(normalizedCode));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalizedName) ||
                (x.ContactName != null && x.ContactName.Contains(normalizedName)));
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var normalizedPhone = phoneNumber.Trim();
            query = query.Where(x => x.PhoneNumber != null && x.PhoneNumber.Contains(normalizedPhone));
        }

        if (!string.IsNullOrWhiteSpace(status) && bool.TryParse(status, out var isActive))
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Min(Math.Max(1, page), totalPages);

        var suppliers = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminSupplierIndexItemViewModel
            {
                SupplierId = x.SupplierId,
                Code = x.Code,
                Name = x.Name,
                ContactName = x.ContactName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Address = x.Address,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt,
                TransactionCount = x.InventoryTransactions.Count
            })
            .ToListAsync();

        return new AdminSupplierIndexViewModel
        {
            Suppliers = suppliers,
            Code = code,
            Name = name,
            PhoneNumber = phoneNumber,
            Status = status,
            Page = currentPage,
            TotalPages = totalPages,
            TotalItems = totalItems,
            QueryString = BuildQueryString(code, name, phoneNumber, status)
        };
    }

    public async Task<AdminSupplierFormViewModel?> GetSupplierFormAsync(int? id)
    {
        if (!id.HasValue)
        {
            return new AdminSupplierFormViewModel();
        }

        return await _context.Suppliers
            .AsNoTracking()
            .Where(x => x.SupplierId == id.Value)
            .Select(x => new AdminSupplierFormViewModel
            {
                SupplierId = x.SupplierId,
                Code = x.Code,
                Name = x.Name,
                ContactName = x.ContactName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Address = x.Address,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveSupplierAsync(AdminSupplierFormViewModel model)
    {
        var normalizedCode = model.Code.Trim().ToUpperInvariant();
        var normalizedName = model.Name.Trim();
        var duplicateExists = model.SupplierId > 0
            ? await _context.Suppliers.AnyAsync(x => x.Code == normalizedCode && x.SupplierId != model.SupplierId)
            : await _context.Suppliers.AnyAsync(x => x.Code == normalizedCode);

        if (duplicateExists)
        {
            throw new InvalidOperationException("Mã nhà cung cấp đã tồn tại.");
        }

        Supplier supplier;
        if (model.SupplierId > 0)
        {
            supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.SupplierId == model.SupplierId)
                ?? throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");
        }
        else
        {
            supplier = new Supplier
            {
                CreatedAt = DateTime.Now
            };
            await _context.Suppliers.AddAsync(supplier);
        }

        supplier.Code = normalizedCode;
        supplier.Name = normalizedName;
        supplier.ContactName = string.IsNullOrWhiteSpace(model.ContactName) ? null : model.ContactName.Trim();
        supplier.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        supplier.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        supplier.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        supplier.IsActive = model.IsActive;
        supplier.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> DeleteSupplierAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(x => x.InventoryTransactions)
            .FirstOrDefaultAsync(x => x.SupplierId == id);

        if (supplier == null)
        {
            return (false, "Không tìm thấy nhà cung cấp.");
        }

        if (supplier.InventoryTransactions.Any())
        {
            return (false, "Nhà cung cấp đã phát sinh phiếu nhập/xuất, không thể xóa.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa nhà cung cấp.");
    }

    private static string BuildQueryString(string? code, string? name, string? phoneNumber, string? status)
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(code))
        {
            values.Add($"code={Uri.EscapeDataString(code)}");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            values.Add($"name={Uri.EscapeDataString(name)}");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            values.Add($"phoneNumber={Uri.EscapeDataString(phoneNumber)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            values.Add($"status={Uri.EscapeDataString(status)}");
        }

        return string.Join("&", values);
    }
}
