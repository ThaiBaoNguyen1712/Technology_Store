# Today Features - 2026-05-18

## Mục tiêu của đợt cập nhật

Đợt cập nhật hôm nay tập trung vào việc nâng cấp nền tảng backend, tái cấu trúc admin workflow, làm mới storefront và chuẩn hóa tài nguyên frontend để dự án dễ bảo trì hơn.

## Các nhóm chức năng đã hoàn thành

### 1. Chuẩn hóa tài nguyên và cleanup repo

- Xóa các file sample/test thừa của `CKEditor`
- Loại bỏ các file SQL backup không còn là runtime artifact
- Bổ sung bộ frontend vendor self-hosted trong `wwwroot/vendor`
- Giảm phụ thuộc trực tiếp vào CDN ở layout storefront và admin

### 2. Nâng cấp backend core

- Tách business logic lớn ra khỏi controller sang service layer
- Bổ sung interface theo domain cho `Auth` và `Payment`
- Thêm `Cloudflare Turnstile` thay cho provider captcha cũ
- Chuẩn hóa lại payment flow thông qua service riêng
- Bổ sung các `enum`, `constant`, `DTO`, `view model` mới cho các flow nghiệp vụ

### 3. Hệ thống payment mới

- Thêm service cho `SePay`
- Tách checkout và callback handling ra thành các service payment chuyên biệt
- Bổ sung màn hình quản trị trạng thái cổng thanh toán
- Cập nhật email/invoice template để hỗ trợ subtotal, discount, total rõ ràng hơn

### 4. Theo dõi hành vi người dùng và recommendation support

- Thêm tracking cho sự kiện xem sản phẩm, thao tác giỏ hàng và tương tác tìm kiếm
- Bổ sung middleware theo dõi hoạt động request của user đã đăng nhập
- Thêm mô hình `UserProductEvent` và queue/background processing cho event tracking
- Cập nhật recommendation flow để dùng event-based data tốt hơn

### 5. Banner management và homepage content service

- Thêm module quản trị banner trong admin
- Bổ sung entity và mapping cho banner position, banner target, brand scope
- Thêm service query banner cho storefront
- Thêm homepage content service để gom dữ liệu banner, category, hot search, brand, hot sale
- Thay banner hardcoded ở storefront bằng banner render từ query/service layer

### 6. Quản lý kho và nhà cung cấp

- Làm lại stock management theo mô hình page-based thay vì modal-based
- Thêm module quản lý `Supplier`
- Hỗ trợ phiếu nhập kho gắn nhà cung cấp
- Thêm màn hình tạo/sửa phiếu nhập xuất kho
- Thêm màn export selection và quick drawer cho stock workflow

### 7. Cải tổ admin UI

- Làm mới admin shell và sidebar
- Cập nhật navigation cho các module mới như supplier và banner
- Thay nhiều flow CRUD cũ từ modal sang page/form workflow
- Tổ chức lại các trang user, transaction, notification, POS, stock theo pattern đồng nhất hơn

### 8. Cải tổ storefront UI

- Viết lại layout storefront theo hướng dùng vendor self-hosted
- Thêm interaction tracker phía client
- Làm mới trang chủ, khu vực account, wishlist, cart, order history, order detail
- Cải thiện responsive layout, empty state và khả năng điều hướng trên mobile
- Cập nhật search suggestion và notification interaction

## File và module nổi bật được thêm mới

- `Services/Auth/*`
- `Services/Payment/*`
- `Services/Recommendation/*`
- `Services/Client/Storefront/*`
- `Services/Client/BannerQueryService.cs`
- `Controllers/BannerController.cs`
- `Controllers/UserInteractionController.cs`
- `Areas/Admin/Controllers/BannersController.cs`
- `Areas/Admin/Controllers/SuppliersController.cs`
- `Views/Shared/Banners/*`
- `Areas/Admin/Views/Banners/*`
- `Areas/Admin/Views/Suppliers/*`

## Migration mới

- `20260506025521_AddSuppliersToInventoryTransactions`
- `20260506040527_AddBannerManagementModule`
- `20260506041803_SeedBannerCategoryBrandContent`
- `20260513093741_AddBannerBrandScopeMapping`
- `20260516042355_AddUserProductEventTracking`
- `20260516045404_AddUserLoginAndRequestMetadata`

## Tác động kiến trúc

- Dự án dịch chuyển rõ hơn sang mô hình controller mỏng và service theo domain
- Admin và storefront đã có cấu trúc layout và asset ổn định hơn
- Các nghiệp vụ payment, tracking, banner, stock và supplier đã có boundary rõ ràng hơn để tiếp tục mở rộng
