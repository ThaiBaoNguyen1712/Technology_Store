# Tech_Store

Tech_Store is a full-stack e-commerce platform for selling mobile devices and consumer tech, built with ASP.NET Core MVC on .NET 8. The project includes a customer-facing storefront, a back-office admin area, inventory and POS workflows, configurable payment gateways, banner management, and user activity tracking to support recommendation features.

## What the project includes

### Storefront

- Homepage with admin-managed banner zones
- Product catalog by category and brand
- Search with suggestions and interaction tracking
- Product detail, wishlist, cart, and checkout
- Customer authentication, OTP, and password recovery
- Account area, order history, order detail, and product reviews

### Admin

- Product, category, brand, and voucher management
- POS workflow and invoice rendering
- Stock management with import/export transactions
- Supplier management
- Storefront banner management
- User, transaction, and notification management
- Payment gateway settings management

### Platform capabilities

- Multiple payment flows: VNPay, Momo, SePay
- Redis-backed search/recommendation support
- SignalR real-time notifications
- Email and invoice template support
- User activity and product event tracking

## Tech stack

- Backend: ASP.NET Core MVC, Entity Framework Core, SQL Server
- Frontend: Razor Views, Bootstrap, jQuery
- Realtime: SignalR
- Cache and support services: Redis
- Email: MailKit, MimeKit
- Payments: VNPay, Momo, SePay

## Project structure

```text
Tech_Store/
|-- Areas/Admin/        # Admin controllers and views
|-- Controllers/        # Storefront controllers
|-- Middleware/         # Request and activity tracking middleware
|-- Migrations/         # EF Core migrations
|-- Models/             # Entities, DTOs, enums, constants, view models
|-- Services/           # Business services by domain
|-- Views/              # Storefront Razor views
|-- wwwroot/            # Static assets, uploads, templates, vendor files
|-- Program.cs          # Bootstrap, DI, middleware pipeline
```

## Documentation in the repo

- `PROJECT_MAP.md`: module map, source layout, and important entry points
- `CODE_CONTEXT.md`: architecture, UI, pagination, form, and maintenance rules
- `TODAY_FEATURES_2026-05-18.md`: summary of the major features added in the latest update batch

## Run locally

### Requirements

- .NET SDK 8
- SQL Server
- Redis

### Configuration

Use these files as the baseline for environment setup:

- `appsettings.template.json`
- `appsettingsTemplate.json`
- `appsettingsTemplate.template.json`

You will typically need to provide:

- SQL Server connection string
- Redis connection settings
- SMTP email settings
- Google and Facebook auth keys
- VNPay, Momo, and SePay settings
- Cloudflare Turnstile settings

### Commands

```powershell
dotnet restore
dotnet ef database update
dotnet build
dotnet run --launch-profile https
```

## Notes

- Main frontend vendor assets are self-hosted under `wwwroot/vendor`
- Local-only artifacts such as `.tmp-build/`, `.vscode/`, and `run.err` should not be committed
- When extending the codebase, read `PROJECT_MAP.md` and `CODE_CONTEXT.md` first
