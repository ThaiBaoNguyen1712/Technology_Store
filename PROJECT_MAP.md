# PROJECT MAP

## Summary

### Tech Stack
- ASP.NET Core MVC on `.NET 8` for the web application framework.
- Entity Framework Core + SQL Server for persistence and database migrations.
- Razor Views for server-rendered UI in both storefront and admin area.
- SignalR for real-time notifications.
- Redis via `StackExchange.Redis` for caching and recommendation/search support.
- MediatR for in-process event/message handling.
- MailKit/MimeKit for email delivery.
- VNPay and Momo integrations for payments.
- Bootstrap, jQuery, Font Awesome, CKEditor/CKFinder for frontend/admin UI.
- Docker support via `Dockerfile` and `docker-compose.yml`.

### Main Modules
- Storefront: customer-facing catalog, product detail, search, cart, checkout, account.
- Admin Area: product, order, category, brand, voucher, stock, POS, settings, users, chat.
- Domain Model: EF Core entities, DTOs, view models, and DB context.
- Services: admin business services, payment gateways, email, Redis, sitemap, recommendations.
- Realtime & Notifications: SignalR hub, notification events/handlers, client notification scripts.
- Cross-cutting Infrastructure: middleware, helpers, extensions, configuration, deployment.

### Entry Points
- **`Program.cs`**: application bootstrap, DI registration, middleware pipeline, routes, SignalR hub mapping.
- **`Tech_Store.csproj`**: target framework, NuGet dependencies, Docker metadata.
- **`Tech_Store.sln`**: solution entry for IDE/build tooling.
- **`Views/Shared/_Layout.cshtml`**: storefront shell layout.
- **`Areas/Admin/Views/Shared/Layout.cshtml`**: admin shell layout.

### Key Services / Classes
- **`Models/ApplicationDbContext.cs`**: EF Core database context and entity mapping root.
- **`Controllers/HomeController.cs`**: storefront landing page, category/product presentation, product detail flow.
- **`Controllers/PaymentController.cs`**: checkout and payment result handling.
- **`Controllers/UserController.cs`**: cart, wishlist, orders, profile/customer actions.
- **`Areas/Admin/Controllers/ProductsController.cs`**: admin product management endpoints.
- **`Areas/Admin/Controllers/OrdersController.cs`**: admin order operations and workflow.
- **`Services/Admin/ProductServices/AdminProductService.cs`**: admin product business logic.
- **`Services/Admin/ProductServices/ProductServices.cs`**: broader product-related operations used by admin/store flows.
- **`Services/Client/RecommendServices.cs`**: recommendation integration for storefront scenes.
- **`Services/Admin/NotificationServices/NotificationService.cs`**: notification orchestration.
- **`Services/RedisService.cs`**: Redis-backed cache/search/recommendation helpers.
- **`Hubs/NotificationHub.cs`**: realtime notification hub for SignalR clients.

## Project Tree

> Notes:
> - Tree depth is intentionally limited to keep the map readable.
> - `node_modules`, `bin`, `obj`, `dist`, and `build` are intentionally omitted.
> - Large generated/static content folders are summarized instead of listing every binary asset.

```text
Tech_Store/
├─ .config/
│  └─ dotnet-tools.json
├─ .github/
├─ Areas/
│  └─ Admin/
│     ├─ Controllers/
│     └─ Views/
├─ Controllers/
├─ Event/
├─ Extensions/
├─ Handles/
├─ Helpers/
├─ Hubs/
├─ Middleware/
├─ Migrations/
├─ Models/
│  ├─ DTO/
│  ├─ Error/
│  ├─ Transfer/
│  └─ ViewModel/
├─ Properties/
│  ├─ PublishProfiles/
│  └─ ServiceDependencies/
├─ Services/
│  ├─ Admin/
│  └─ Client/
├─ sql/
├─ Views/
│  ├─ Authentication/
│  ├─ Error/
│  ├─ Home/
│  ├─ Payment/
│  ├─ Shared/
│  └─ User/
└─ wwwroot/
   ├─ Admin/
   ├─ Client/
   ├─ css/
   ├─ js/
   ├─ json/
   ├─ lib/
   ├─ sounds/
   ├─ Template/
   └─ Upload/
```

## Root And Configuration

- **`Program.cs`**: main application bootstrap, service registration, auth/session setup, middleware pipeline, routes, SignalR hub.
- **`Tech_Store.csproj`**: project definition, `.NET 8` target, package references, Docker settings.
- `Tech_Store.sln`: Visual Studio solution containing the web application project.
- `README.md`: repository overview and basic project notes.
- `ScaffoldingReadMe.txt`: scaffold-generated notes from ASP.NET tooling.
- `package.json`: minimal frontend package manifest used for JavaScript package management.
- `package-lock.json`: npm dependency lockfile.
- `appsettings.json`: primary runtime configuration for connection strings, auth keys, payment, email, Redis.
- `appsettings.Development.json`: development-only configuration overrides.
- `appsettings.template.json`: template config for bootstrapping deployments/environments.
- `appsettingsTemplate.json`: alternate appsettings template snapshot kept in repo.
- `appsettingsTemplate.Development.json`: development version of the template config.
- `appsettingsTemplate.template.json`: additional configuration template variant.
- `.dockerignore`: excludes files from Docker build context.
- `Dockerfile`: container build recipe for the ASP.NET app.
- `Dockerfile.original`: backup/original Docker build file.
- `docker-compose.yml`: local or deployment multi-container orchestration file.
- `.gitignore`: Git exclusions for build output, secrets, and generated files.
- `.gitattributes`: repository text/binary handling rules.
- `.config/dotnet-tools.json`: local dotnet tool manifest for CLI tooling.
- `Tech_Store.csproj.user`: user-specific IDE project settings.

## Storefront Module

### Controllers
- **`Controllers/BaseController.cs`**: shared storefront base controller with common context/view setup.
- **`Controllers/HomeController.cs`**: homepage, category listing, product detail, and discovery/search related actions.
- `Controllers/AuthenticationController.cs`: login, register, OTP, password recovery, and account auth flow.
- `Controllers/UserController.cs`: cart, wishlist, account details, and customer order operations.
- `Controllers/PaymentController.cs`: checkout workflow, payment callback/return handling, and order completion.
- `Controllers/NotificationsController.cs`: customer notification read/update endpoints.
- `Controllers/RecomendationController.cs`: recommendation-related storefront endpoints.
- `Controllers/ErrorController.cs`: not-found and error rendering entry points.

### Views
- `Views/_ViewImports.cshtml`: shared Razor imports, namespaces, and tag helpers.
- `Views/_ViewStart.cshtml`: default Razor layout configuration.

#### Authentication
- `Views/Authentication/Login.cshtml`: customer sign-in page.
- `Views/Authentication/Register.cshtml`: customer registration page.
- `Views/Authentication/ForgotPassword.cshtml`: forgot-password request form.
- `Views/Authentication/ChangePassword.cshtml`: password reset/change screen.
- `Views/Authentication/VerifyOTP.cshtml`: OTP verification UI.

#### Home / Catalog
- `Views/Home/Index.cshtml`: storefront home page with featured sections and recommendations.
- `Views/Home/Categories.cshtml`: category overview listing page.
- `Views/Home/Category.cshtml`: single category product listing page.
- `Views/Home/Brand.cshtml`: brand-focused product listing page.
- `Views/Home/Search.cshtml`: search results page.
- `Views/Home/View.cshtml`: product detail page.

#### Payment
- `Views/Payment/Checkout.cshtml`: checkout page and payment method selection UI.
- `Views/Payment/OrderSuccess.cshtml`: successful order confirmation screen.
- `Views/Payment/PaymentSuccess.cshtml`: successful payment callback result view.
- `Views/Payment/PaymentFail.cshtml`: failed payment result view.

#### User
- `Views/User/Cart.cshtml`: shopping cart screen.
- `Views/User/Information.cshtml`: customer profile/account info page.
- `Views/User/MyOrders.cshtml`: customer order history page.
- `Views/User/OrderDetail.cshtml`: individual customer order detail page.
- `Views/User/Wishlist.cshtml`: favorite products page.

#### Shared
- **`Views/Shared/_Layout.cshtml`**: primary storefront layout, navbar, search, notification shell, and footer.
- `Views/Shared/_Layout.cshtml.css`: layout-scoped CSS generated by ASP.NET styling conventions.
- `Views/Shared/_ValidationScriptsPartial.cshtml`: client validation scripts partial.
- `Views/Shared/ToastLive.cshtml`: toast/notification partial for transient messages.
- `Views/Shared/LoginNoti.cshtml`: login-required modal/notice partial.
- `Views/Shared/ChatWindow.cshtml`: embedded customer chat UI partial.
- `Views/Shared/additionalInformationRqModal.cshtml`: modal requesting missing customer profile information.
- `Views/Shared/Error.cshtml`: shared fallback error view.

#### Error
- `Views/Error/Error.cshtml`: application error page.
- `Views/Error/NotFound.cshtml`: 404 not-found page.

## Admin Module

### Controllers
- **`Areas/Admin/Controllers/BaseAdminController.cs`**: shared admin base controller with admin context/authorization support.
- `Areas/Admin/Controllers/LoginController.cs`: admin authentication entry page/controller.
- `Areas/Admin/Controllers/HomeController.cs`: admin dashboard and overview endpoints.
- `Areas/Admin/Controllers/ProductsController.cs`: product CRUD, imports, attributes, and detail management.
- `Areas/Admin/Controllers/OrdersController.cs`: order management, order detail, and admin workflow actions.
- `Areas/Admin/Controllers/CategoriesController.cs`: category management endpoints.
- `Areas/Admin/Controllers/BrandsController.cs`: brand management endpoints.
- `Areas/Admin/Controllers/AttributesController.cs`: product attribute/option management.
- `Areas/Admin/Controllers/UsersController.cs`: customer/user administration.
- `Areas/Admin/Controllers/VouchersController.cs`: coupon/voucher administration.
- `Areas/Admin/Controllers/StockManagerController.cs`: inventory movement and stock audit operations.
- `Areas/Admin/Controllers/TransactionsController.cs`: payment/transaction tracking and detail views.
- `Areas/Admin/Controllers/NotificationsController.cs`: admin notification management.
- `Areas/Admin/Controllers/ChatsController.cs`: customer/admin chat management.
- `Areas/Admin/Controllers/POSController.cs`: point-of-sale order creation and invoice flow.
- `Areas/Admin/Controllers/SettingsController.cs`: site/system settings administration.
- `Areas/Admin/Controllers/AccountController.cs`: admin account profile features.

### Views
- **`Areas/Admin/Views/Shared/Layout.cshtml`**: admin layout shell, navigation, and shared scripts/styles.
- `Areas/Admin/Views/Shared/ToastLive.cshtml`: admin toast message partial.
- `Areas/Admin/Views/Login/Index.cshtml`: admin login page.
- `Areas/Admin/Views/Home/Index.cshtml`: admin dashboard page.
- `Areas/Admin/Views/Account/Index.cshtml`: admin account page.

#### Products
- `Areas/Admin/Views/Products/Index.cshtml`: product listing and management grid.
- `Areas/Admin/Views/Products/Create.cshtml`: create product form.
- `Areas/Admin/Views/Products/Edit.cshtml`: edit product form.
- `Areas/Admin/Views/Products/View.cshtml`: admin product detail/preview page.
- `Areas/Admin/Views/Products/AttributeModal.cshtml`: modal for product attribute editing.
- `Areas/Admin/Views/Products/ImportExcel.cshtml`: bulk import UI for product spreadsheets.
- `Areas/Admin/Views/Products/GenerateCode.cshtml`: code/QR/barcode generation view.

#### Orders / POS / Stock
- `Areas/Admin/Views/Orders/Index.cshtml`: admin order list.
- `Areas/Admin/Views/Orders/View.cshtml`: admin order detail page.
- `Areas/Admin/Views/POS/Index.cshtml`: POS sales screen.
- `Areas/Admin/Views/POS/ViewProduct.cshtml`: POS product chooser/detail view.
- `Areas/Admin/Views/POS/Invoice.cshtml`: POS invoice output page.
- `Areas/Admin/Views/POS/ModalDeduct.cshtml`: POS discount/deduction modal.
- `Areas/Admin/Views/POS/ModalDiscount.cshtml`: POS discount modal variant.
- `Areas/Admin/Views/POS/ModalUser.cshtml`: POS customer selection modal.
- `Areas/Admin/Views/StockManager/Index.cshtml`: stock management main page.
- `Areas/Admin/Views/StockManager/History.cshtml`: stock movement history page.
- `Areas/Admin/Views/StockManager/ModalAddHistory.cshtml`: manual stock history entry modal.
- `Areas/Admin/Views/StockManager/ModalImExport.cshtml`: import/export stock operation modal.

#### Catalog / Users / Finance / Settings
- `Areas/Admin/Views/Categories/Index.cshtml`: category list page.
- `Areas/Admin/Views/Categories/Detail.cshtml`: category detail page.
- `Areas/Admin/Views/Categories/Form.cshtml`: category form page.
- `Areas/Admin/Views/Categories/ModalCategory.cshtml`: category modal form.
- `Areas/Admin/Views/Brands/Index.cshtml`: brand list page.
- `Areas/Admin/Views/Brands/Detail.cshtml`: brand detail page.
- `Areas/Admin/Views/Brands/ModalBrand.cshtml`: brand modal form.
- `Areas/Admin/Views/Users/Index.cshtml`: user management page.
- `Areas/Admin/Views/Users/ModalUser.cshtml`: user modal form.
- `Areas/Admin/Views/Vouchers/Index.cshtml`: voucher list page.
- `Areas/Admin/Views/Vouchers/ModalVoucher.cshtml`: voucher modal form.
- `Areas/Admin/Views/Transactions/Index.cshtml`: transaction list page.
- `Areas/Admin/Views/Transactions/Detail.cshtml`: transaction detail page.
- `Areas/Admin/Views/Notifications/Index.cshtml`: notification management page.
- `Areas/Admin/Views/Chats/Index.cshtml`: admin chat inbox page.
- `Areas/Admin/Views/Settings/Index.cshtml`: site settings page.

## Domain Model And Data Layer

### Core Data Access
- **`Models/ApplicationDbContext.cs`**: central EF Core context exposing entities and database mappings.
- `Migrations/ApplicationDbContextModelSnapshot.cs`: latest EF Core model snapshot.

### Core Entities
- `Models/User.cs`: user/customer entity with auth and profile-related fields.
- `Models/Role.cs`: role entity used for authorization.
- `Models/Setting.cs`: site-wide settings/configuration stored in DB.
- `Models/Category.cs`: product category entity.
- `Models/Brand.cs`: brand entity tied to products/categories.
- `Models/Product.cs`: core product entity with pricing, stock, and metadata.
- `Models/VarientProduct.cs`: product variant entity for SKU-level combinations.
- `Models/VariantAttribute.cs`: link between variants and attribute values.
- `Models/Attribute.cs`: attribute definition entity for product options/specification grouping.
- `Models/AttributeValue.cs`: value entity for attribute options.
- `Models/ProductAttribute.cs`: association entity connecting products to attributes.
- `Models/Gallery.cs`: product image gallery entity.
- `Models/Banner.cs`: homepage/banner content entity.
- `Models/Specs.cs`: specification definition entity.
- `Models/SpecValue.cs`: specification value entity assigned to products.
- `Models/Comment.cs`: customer discussion/comment entity for products.
- `Models/Review.cs`: product rating/review entity.
- `Models/Wishlist.cs`: user wishlist/favorites entity.
- `Models/Cart.cs`: shopping cart container entity.
- `Models/CartItem.cs`: cart line-item entity.
- `Models/Order.cs`: order header entity.
- `Models/OrderItem.cs`: order line-item entity.
- `Models/Payment.cs`: payment transaction entity.
- `Models/Voucher.cs`: discount voucher/coupon entity.
- `Models/Notification.cs`: system notification entity.
- `Models/UserNotification.cs`: user-specific notification read state entity.
- `Models/Address.cs`: address entity for customers/orders.
- `Models/InventoryTransactions.cs`: stock movement header entity.
- `Models/InventoryTransactionsDetail.cs`: stock movement detail entity.
- `Models/InventoryTransactionsVM.cs`: inventory transaction-focused view models.
- `Models/ErrorViewModel.cs`: basic MVC error view model.

### DTOs And View Models
- `Models/DTO/AdminProductDtos.cs`: DTOs used by admin product workflows.
- `Models/DTO/AttributeDTo.cs`: DTOs for attributes and values.
- `Models/DTO/CartDTo.cs`: cart transfer models.
- `Models/DTO/CommentDTo.cs`: comment request/response data models.
- `Models/DTO/OrderItemsDTo.cs`: order item transfer models.
- `Models/DTO/ProductDTo.cs`: product transfer models.
- `Models/DTO/ProductHistoryDTo.cs`: product history/audit DTOs.
- `Models/DTO/ProductViewModel.cs`: product projection model for UI/API transfer.
- `Models/DTO/Province.cs`: province/location DTO.
- `Models/DTO/RecommendDTo.cs`: recommendation request/response DTOs.
- `Models/DTO/SettingDTo.cs`: settings transfer models.
- `Models/DTO/UserDTo.cs`: user transfer models.
- `Models/DTO/VariantViewModel.cs`: variant projection model for UI.
- `Models/DTO/VarientProductDTo.cs`: variant transfer models.
- `Models/DTO/Authentication/LoginDTo.cs`: login request DTO.
- `Models/DTO/Authentication/RegisterDTo.cs`: register request DTO.
- `Models/DTO/Authentication/ForgotPasswordDTo.cs`: forgot-password request DTO.
- `Models/DTO/Authentication/ChangePasswordDTo.cs`: password change/reset DTO.
- `Models/DTO/Authentication/VerifyOtpDTo.cs`: OTP verification DTO.
- `Models/DTO/Chat/ChatMessage.cs`: chat message transfer model.
- `Models/DTO/Payment/Admin/InvoicesDTo.cs`: admin invoice transfer model.
- `Models/DTO/Payment/Admin/PaymentLinkDTo.cs`: admin payment link DTO.
- `Models/DTO/Payment/Client/CheckoutDTo.cs`: client checkout DTO.
- `Models/DTO/Payment/Client/PaymentDTo.cs`: client payment DTO.
- `Models/DTO/Payment/Client/Momo/MomoExecuteResponseModel.cs`: Momo execution response model.
- `Models/DTO/Payment/Client/Momo/MomoPaymentResponseModel.cs`: Momo payment response model.
- `Models/DTO/Payment/Client/VnPay/VNPaymentResponseModel.cs`: VNPay response model.
- `Models/ViewModel/CommentVM.cs`: nested comment view model for product pages.
- `Models/ViewModel/DeleteFileViewModel.cs`: file deletion-related view model.
- `Models/ViewModel/OrderDetailVM.cs`: order detail presentation model.
- `Models/ViewModel/OrderViewModel.cs`: admin/order overview view model.
- `Models/ViewModel/OrderViewModelClient.cs`: customer-facing order view model.
- `Models/ViewModel/TransactionVM.cs`: transaction presentation model.
- `Models/ViewModel/UserVM.cs`: user presentation model.
- `Models/ViewModel/WishlistVM.cs`: wishlist presentation model.
- `Models/Transfer/Variant_TF.cs`: variant transfer/helper structure.
- `Models/Error/ImportError.cs`: import validation/error reporting model.

### Migrations
- `Migrations/20260101121514_AddProductSpecs.cs`: adds product specification schema support.
- `Migrations/20260101122609_createTableSpecs.cs`: creates specification-related tables.
- `Migrations/20260106112256_AddSpecsValue.cs`: extends schema with spec values.
- `Migrations/20260106115044_RemaneHistoryPRD-To-Inven....cs`: renames inventory/history table structures.
- `Migrations/20260106115942_AddImageUrl-VariantPrd.cs`: adds variant image URL support.
- `Migrations/20260110101508_ResizePrices.cs`: adjusts numeric precision/size for prices.
- `Migrations/20260111124915_CreateBannerTable.cs`: creates banner table.
- `Migrations/20260113073735_AddProduct-Sys-IDColumns.cs`: adds product system ID columns.
- `Migrations/20260114051131_AddCategoryChild.cs`: adds category hierarchy/child support.
- `Migrations/20260119161616_FixProductIndex.cs`: fixes product indexing/schema details.
- `Migrations/*.Designer.cs`: EF-generated migration designer snapshots for corresponding migrations.

## Services And Business Logic

### Cross-Cutting Services
- `Services/IEmailService.cs`: email service contract.
- `Services/EmailService.cs`: SMTP/email sending implementation.
- `Services/RedisService.cs`: Redis access wrapper for counters, search signals, or recommendation helpers.
- `Services/SitemapService.cs`: sitemap generation/update service.

### Client Services
- **`Services/Client/RecommendServices.cs`**: storefront recommendation service for scene-based product suggestions.

### Admin Service Interfaces
- `Services/Admin/Interfaces/IAdminProductService.cs`: contract for admin product operations.
- `Services/Admin/Interfaces/IBrandService.cs`: contract for brand operations.
- `Services/Admin/Interfaces/ICategoryService.cs`: contract for category operations.
- `Services/Admin/Interfaces/IVoucherService.cs`: contract for voucher operations.

### Admin Implementations
- `Services/Admin/BrandServices/BrandService.cs`: business logic for brand management.
- `Services/Admin/CategoryServices/CategoryService.cs`: business logic for category management.
- `Services/Admin/VoucherServices/VoucherService.cs`: business logic for voucher administration.
- **`Services/Admin/ProductServices/AdminProductService.cs`**: core admin product CRUD and orchestration service.
- `Services/Admin/ProductServices/ProductServices.cs`: supporting product operations shared across flows.
- `Services/Admin/ProductServices/ExcelService.cs`: spreadsheet import/export helpers for products.
- `Services/Admin/NotificationServices/NotificationService.cs`: creates and manages admin/store notifications.

### Payment Services
- `Services/Admin/VNPayServices/IVnPayService.cs`: VNPay gateway service contract.
- `Services/Admin/VNPayServices/VnPayService.cs`: VNPay integration implementation.
- `Services/Admin/MomoServices/IMomoService.cs`: Momo gateway service contract.
- `Services/Admin/MomoServices/MomoService.cs`: Momo payment integration implementation.

## Realtime, Events, Middleware, Helpers

### Realtime / Events
- **`Hubs/NotificationHub.cs`**: SignalR hub for pushing notifications to connected clients.
- `Hubs/OnlineUserTracker.cs`: tracks connected/online users for realtime flows.
- `Event/NotificationEvent.cs`: MediatR event/message for notification processing.
- `Handles/NotificationHandler.cs`: handles notification events and downstream processing.

### Middleware
- `Middleware/AuthMiddleware.cs`: custom authentication-related request middleware.
- `Middleware/GuestSessionMiddleware.cs`: establishes guest session behavior before auth.
- `Middleware/TrackProductViewMiddleware.cs`: records product view tracking during requests.

### Helpers
- `Helpers/DateTimeExtensions.cs`: time formatting helpers such as relative time.
- `Helpers/EmailSettings.cs`: strongly typed email configuration object.
- `Helpers/InvoiceEmail.cs`: invoice email composition helper.
- `Helpers/MailRequest.cs`: email request payload model/helper.
- `Helpers/PasswordHelper.cs`: password hashing/validation helper logic.
- `Helpers/ResizeCDN.cs`: image/CDN resize utility helpers.
- `Helpers/VNPayLibrary.cs`: VNPay-specific signing/query helper functions.

### Extensions
- `Extensions/SelectHelper.cs`: Razor/UI helper extensions for select controls.
- `Extensions/SessionExtensions.cs`: typed session read/write helper extensions.

## Infrastructure And Deployment

- `Properties/launchSettings.json`: local launch profiles for IIS Express/Kestrel.
- `Properties/serviceDependencies.json`: service dependency metadata for deployment tooling.
- `Properties/serviceDependencies.local.json`: local service dependency configuration.
- `Properties/serviceDependencies.local.json.user`: user-scoped local dependency settings.
- `Properties/PublishProfiles/FolderProfile.pubxml`: folder publish profile.
- `Properties/PublishProfiles/TechShop - FTP.pubxml`: FTP publish profile.
- `Properties/PublishProfiles/TechShop - ReadOnly - FTP.pubxml`: read-only FTP deployment profile.
- `Properties/PublishProfiles/TechShop - Web Deploy.pubxml`: web deploy profile.
- `Properties/PublishProfiles/TechShop - Zip Deploy.pubxml`: zip deploy profile.
- `Properties/PublishProfiles/*.user`: user-specific publish profile settings.
- `Properties/ServiceDependencies/local/mssql1.arm.json`: local SQL dependency metadata.
- `Properties/ServiceDependencies/*/profile.arm.json`: deployment dependency descriptors generated by tooling.
- `sql/restore.sql`: SQL restore/setup script.
- `sql/techstore.bak`: SQL Server backup file for the project database.

## Static Assets And Frontend Resources

### Shared Public Assets
- `wwwroot/css/site.css`: default shared site CSS generated/maintained by ASP.NET template conventions.
- `wwwroot/js/site.js`: default shared site JavaScript entry.
- `wwwroot/js/notification.js`: notification behavior script.
- `wwwroot/js/notificationClientSide.js`: client-side realtime notification handling script.
- `wwwroot/js/provincevn.js`: province/location dataset helper script.
- `wwwroot/js/thirdparties.js`: glue code for third-party frontend integrations.
- `wwwroot/json/products.json`: product data feed used by client-side search/autocomplete.
- `wwwroot/sounds/notification.mp3`: sound asset for notification playback.

### Storefront Assets
- `wwwroot/Client/asset/css/site.css`: main storefront CSS bundle/custom styles.
- `wwwroot/Client/asset/css/mobile-fix.css`: mobile-specific storefront CSS overrides.
- `wwwroot/Client/asset/css/categoryview.css`: category listing page styles.
- `wwwroot/Client/asset/css/detailProduct.css`: product detail page styles.
- `wwwroot/Client/asset/css/ordersview.css`: order-related customer page styles.
- `wwwroot/Client/asset/css/pagination.css`: pagination component styles.
- `wwwroot/Client/asset/css/searchView.css`: search page styles.
- `wwwroot/Client/asset/js/index.js`: primary storefront interactive behavior.
- `wwwroot/Client/asset/js/recommendation.js`: storefront recommendation UI script.
- `wwwroot/Client/asset/img/*`: storefront promotional and decorative images.

### Admin Assets
- `wwwroot/Admin/assets/css/site.css`: admin custom stylesheet.
- `wwwroot/Admin/assets/css/kaiadmin.css`: admin theme base styles.
- `wwwroot/Admin/assets/css/bootstrap.min.css`: bundled Bootstrap for admin UI.
- `wwwroot/Admin/assets/js/*`: admin theme and interaction scripts.
- `wwwroot/Admin/ckfinder/*`: CKFinder file manager distribution and user files.
- `wwwroot/Admin/components/*.html`: admin theme sample/component pages.
- `wwwroot/Admin/forms/*.html`: admin sample form pages.
- `wwwroot/Admin/maps/*.html`: admin sample map pages.
- `wwwroot/Admin/tables/*.html`: admin sample table pages.

### Templates And Uploads
- `wwwroot/Template/invoice.html`: invoice HTML template.
- `wwwroot/Template/print-code-template.html`: printable code/label template.
- `wwwroot/Template/ThanksAndInvoice.html`: thank-you and invoice email/template HTML.
- `wwwroot/Template/SampleFiles/ImportProducts_sample.xlsx`: sample import spreadsheet.
- `wwwroot/Upload/Avatar/*`: uploaded user avatar files.
- `wwwroot/Upload/Banner/*`: uploaded banner assets.
- `wwwroot/Upload/Logo/*`: uploaded logo/brand/category image assets.
- `wwwroot/Upload/Products/*`: uploaded product media assets.

### Vendor Libraries
- `wwwroot/lib/bootstrap/*`: Bootstrap frontend library distribution.
- `wwwroot/lib/jquery/*`: jQuery frontend library distribution.
- `wwwroot/lib/jquery-validation/*`: jQuery validation library distribution.
- `wwwroot/lib/jquery-validation-unobtrusive/*`: ASP.NET unobtrusive validation scripts.

