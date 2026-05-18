# Tech_Store

## Tổng quan

`Tech_Store` là một hệ thống e-commerce bán thiết bị công nghệ xây dựng bằng `ASP.NET Core MVC` trên `.NET 8`.
Project gồm hai khối chính:

- Storefront cho khách hàng: duyệt sản phẩm, tìm kiếm, giỏ hàng, thanh toán, tài khoản, đơn hàng.
- Admin backoffice: quản lý sản phẩm, kho, POS, banner, voucher, người dùng, giao dịch, cấu hình hệ thống.

## Công nghệ chính

- Backend: `ASP.NET Core MVC`, `Entity Framework Core`, `SQL Server`
- Realtime: `SignalR`
- Cache và hỗ trợ discovery/recommendation: `Redis`
- UI: `Razor Views`, `Bootstrap`, `jQuery`
- Payment: `VNPay`, `Momo`, `SePay`
- Email: `MailKit`, `MimeKit`

## Chức năng chính

### Storefront

- Trang chủ động với banner quản trị được từ admin
- Danh mục, thương hiệu, tìm kiếm và gợi ý tìm kiếm
- Trang chi tiết sản phẩm, wishlist, cart
- Checkout nhiều cổng thanh toán
- Đăng nhập, đăng ký, OTP, quên mật khẩu
- Khu vực tài khoản, lịch sử đơn hàng, chi tiết đơn hàng, đánh giá sản phẩm
- Theo dõi hành vi người dùng để phục vụ recommendation và analytics nội bộ

### Admin

- Quản lý sản phẩm, category, brand, voucher
- Dashboard và điều hướng admin mới
- POS và in hóa đơn
- Quản lý kho theo phiếu nhập/xuất
- Quản lý nhà cung cấp
- Quản lý banner storefront
- Quản lý người dùng, giao dịch, thông báo
- Bật/tắt cổng thanh toán từ trang cấu hình

## Tài liệu trong repo

- `PROJECT_MAP.md`: bản đồ module, cấu trúc source và các entry point quan trọng
- `CODE_CONTEXT.md`: quy ước kiến trúc, UI, pagination, form, layout và workflow bảo trì
- `TODAY_FEATURES_2026-05-18.md`: tổng hợp các chức năng lớn đã hoàn thành trong đợt cập nhật hôm nay

## Cấu trúc thư mục

```text
Tech_Store/
|-- Areas/Admin/        # Admin area: controllers, views, workflows backoffice
|-- Controllers/        # Storefront controllers
|-- Middleware/         # Middleware theo dõi hành vi và request
|-- Migrations/         # EF Core migrations
|-- Models/             # Entities, DTOs, enums, constants, view models
|-- Services/           # Business services theo domain
|-- Views/              # Razor views cho storefront
|-- wwwroot/            # Static assets, templates, uploads, vendor assets
|-- Program.cs          # Bootstrap, DI, middleware pipeline
```

## Chạy project local

### Yêu cầu

- `.NET SDK 8`
- `SQL Server`
- `Redis`

### Chuẩn bị cấu hình

Tạo hoặc cập nhật các file cấu hình môi trường dựa trên:

- `appsettings.template.json`
- `appsettingsTemplate.json`
- `appsettingsTemplate.template.json`

Các nhóm cấu hình chính cần có:

- Connection string SQL Server
- Redis
- Email SMTP
- Google/Facebook auth
- VNPay, Momo, SePay
- Cloudflare Turnstile

### Chạy ứng dụng

```powershell
dotnet restore
dotnet ef database update
dotnet build
dotnet run --launch-profile https
```

## Ghi chú

- Repo hiện có một số file local không nên commit như `.tmp-build/`, `.vscode/`, `run.err`.
- Frontend vendor chính đã được self-host trong `wwwroot/vendor`.
- Khi mở rộng module mới, ưu tiên đọc `PROJECT_MAP.md` và `CODE_CONTEXT.md` trước.
