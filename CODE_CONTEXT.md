# CODE CONTEXT

## Purpose

This file is the maintenance and upgrade context for `Tech_Store`.
It complements `PROJECT_MAP.md` and defines the architectural and UI rules that must stay consistent when adding, refactoring, or extending pages.

Primary goals:
- keep the codebase coherent during long-term maintenance
- avoid each page drifting into a different UI style or implementation pattern
- reduce frontend performance regressions caused by duplicated CSS/JS patterns
- preserve consistent admin and client UX across modules

## Project Shape

- Platform: ASP.NET Core MVC on `.NET 8`
- Rendering: Razor Views
- Persistence: EF Core + SQL Server
- Realtime: SignalR
- Caching/support: Redis
- Main zones:
  - `Views/*`: client storefront
  - `Areas/Admin/*`: admin backoffice
  - `Services/*`: business logic
  - `Models/*`: entities, DTOs, view models, DbContext
  - `wwwroot/*`: static assets

## Source Of Truth

Use these files as the first place to understand or extend shared behavior:

- `Program.cs`
  - middleware, routing, auth/session, status code behavior
- `Models/ApplicationDbContext.cs`
  - entity mapping and EF conventions
- `Views/Shared/_Layout.cshtml`
  - shared storefront shell
- `Areas/Admin/Views/Shared/Layout.cshtml`
  - shared admin shell
- `wwwroot/css/site.css`
  - global shared CSS base
- `wwwroot/Client/asset/css/site.css`
  - client UI layer
- `wwwroot/Admin/assets/css/site.css`
  - admin UI layer

## Architecture Rules

- Controllers should stay thin.
- Business logic belongs in `Services/*`, not in Razor views or large controller actions.
- Khi thêm service interface mới, đặt interface vào folder `Services/Interfaces/*` theo domain/module; không đặt interface rải trực tiếp ở root `Services` hoặc trộn chung với file implement nếu không có lý do đặc biệt.
- Với payment/auth/banner và các module nghiệp vụ lớn, interface phải nằm trong folder domain tương ứng như `Services/Interfaces/Payment`, `Services/Interfaces/Auth`.
- DTO/ViewModel classes should be used to shape admin and client data instead of passing raw entities where view-specific structure is needed.
- Shared admin data should keep flowing through `BaseAdminController`.
- Shared client data should keep flowing through `BaseController`.
- Do not introduce page-specific behavior into global layout unless the feature is truly cross-site.
- Tránh magic value cho `status`, `type`, `method`, `source`, key xử lý nghiệp vụ; ưu tiên `enum` hoặc hằng số dùng chung để controller, service, view model không tự giữ chuỗi rời rạc.

- Luồng lên đơn, thanh toán, trừ tồn và tiêu hao voucher phải chạy trong transaction backend; nếu một bước ghi DB thất bại thì rollback toàn bộ, không commit dở dang.
- Khi chạy ứng dụng local, ưu tiên chạy bằng launch profile của project trong workspace editor (`https`) thay vì spawn process rời; trước khi run lại phải dọn listener cũ của app để tránh phải kill tay.

## UI Consistency Rules

### Typography

- Site-wide storefront font is `Roboto` because it supports Vietnamese well and keeps product/card text compact.
- Do not add a new font per page or module.
- Allowed fallback stack:
  - `"Roboto", "Segoe UI", Arial, sans-serif`

### Layout

- Client pages must reuse the storefront shell in `Views/Shared/_Layout.cshtml`.
- Admin pages must reuse the admin shell in `Areas/Admin/Views/Shared/Layout.cshtml`.
- Do not embed large `<style>` blocks inside views.
- Put reusable CSS in:
  - `wwwroot/Client/asset/css/site.css` for storefront
  - `wwwroot/Admin/assets/css/site.css` for admin
- Shared admin breadcrumb placement belongs in the admin layout, not redefined per page.
- Admin breadcrumb is currently rendered in the footer region of the admin layout, replacing the old copyright slot.

### CSS Naming

- Prefer scoped namespace classes for larger sections.
- Recommended format:
  - `module-name__element`
  - `module-name--modifier`
- Examples already in use:
  - `site-footer__...`
  - `settings-page__...`
  - `settings-panel__...`
  - `admin-error-page__...`
- Avoid writing new generic selectors that can unintentionally affect unrelated pages.

### Components

- Reuse existing admin table and pagination classes:
  - `reusable-admin-table`
  - `reusable-admin-pagination`
- For admin list pages with thumbnails, keep the first column as a compact image column and reuse:
  - `brand-admin__logo-shell`
  - `brand-admin__logo`
- For admin list pages with manual ordering, expose a compact `SortOrder` badge column and keep sorting stable in BE.
- Reuse existing attribute tag display class:
  - `attribute-tag-list`
- Reuse existing manual pagination partial:
  - `Areas/Admin/Views/Shared/_ManualPagination.cshtml`

## Pagination Rules

- Do not introduce new DataTables usage for new admin pages.
- Preferred direction is manual pagination.
- Default admin page size comes from config:
  - `AdminUi:DefaultPageSize`
- Current default:
  - `20`
- When creating new paginated admin pages:
  - paginate server-side when possible
  - reuse `_ManualPagination.cshtml`
  - keep query params stable: `keyword`, `page`, `pageSize`
  - after create/update, return to the same `page` and `pageSize` when the form came from a paginated list
  - do not render all page numbers if there are many pages; use a compact window with ellipsis

## Form Rules

- Large settings or management pages should be grouped by section, not by one long flat form.
- Labels must be in Vietnamese and easy to scan.
- Destructive or important actions should use confirmation dialogs.
- File uploads should provide inline preview where useful.
- For repeated metadata inputs, prefer table-like row editors or dedicated management pages instead of very long stacked inputs.
- Do not add side advisory panels, checklist blocks, or explanatory rule cards to standard CRUD forms unless the user explicitly asks for that content.
- If an entity is already referenced by other tables or business records, do not hard delete it; use soft delete, inactive status, or equivalent non-destructive deactivation instead.

## Admin UI Rules

- Admin UI should feel like an operations tool:
  - dense but readable
  - low decoration
  - clear grouping
  - strong information hierarchy
- Breadcrumbs in admin should:
  - stay inside the shared admin footer area unless the layout standard changes again
  - allow horizontal scrolling when the path is long
  - never collide with header utilities or overlay page content
- Order management pages should follow familiar Vietnamese ecommerce operations patterns:
  - compact filter bar
  - search actions such as `Tìm kiếm` and `Làm mới` stay in the last filter column and align to the end of the filter area on desktop
  - search action buttons must behave as a normal filter column first; if the current row still has available slot, they stay on that same row
  - only when the current row is actually full may the search action buttons wrap to a new row, and in that case the action group must still stay right-aligned
  - server-side manual pagination
  - obvious status/payment badges
  - customer/payment/amount summary first
  - quick order actions should use a reusable right drawer or side panel instead of placing raw update controls directly inside table cells
  - order detail pages should prioritize operational data over invoice-like decoration
- Product administration and stock administration are separate concerns:
  - product list pages focus on CRUD, filters, visibility, and merchandising state
  - stock actions such as import/export inventory and printing product codes belong under inventory/warehouse navigation, not mixed into the main product list action set
- Stock administration pages should follow the same admin list pattern:
  - no DataTables for stock list, stock history, or stock export selection
  - server-side manual pagination via `_ManualPagination.cshtml`
  - compact paired filters with `Tìm kiếm` and `Làm mới` aligned to the right end of the filter section
  - search action buttons must not be forced into a dedicated full-width row when the current filter grid still has available slot
  - inventory import/export create and update flows use dedicated pages, not modal popups
  - import tickets should support supplier linkage because that is standard for ecommerce warehouse intake in Vietnam
  - supplier management belongs under inventory navigation and should use the same search + table + page-form CRUD pattern
- Banner management should follow the same admin pattern:
  - use page-based CRUD, not modal CRUD
  - use manual pagination on the admin list
  - keep banner target logic structured through banner target tables or view models, not free-form mixed fields in the view
  - each storefront banner position must support at least one default fallback banner so expired campaigns do not leave empty gaps
  - storefront pages should render banners from query/service layers, not hardcoded image blocks inside Razor when the position is intended to be managed from admin
- Avoid old-style tab strips when the content is large and multi-section.
- Prefer:
  - left-side local navigation
  - grouped panels
  - action bar at the bottom or top-right
- Modals should be centered with `modal-dialog-centered`.

## Storefront UI Rules

- Keep responsive behavior consistent across header, footer, search, category navigation, and product cards.
- Do not add per-page fonts, button systems, or spacing systems.
- Product, category, cart, and detail pages should inherit the same typography and spacing rules from shared CSS.
- Use icons sparingly in client UI. Prefer clear text labels and product imagery over decorative or AI-looking icon clusters; avoid adding icons that do not improve scanning or action clarity.

## Performance Rules

- Do not duplicate large CSS blocks in multiple pages.
- Avoid inline JS for behaviors that should be reusable across pages.
- Prefer one shared selector pattern instead of one-off IDs when multiple instances of a component exist.
- Avoid loading libraries for a single page unless they are already part of the project standard.
- New admin list pages should not depend on DataTables if manual pagination is enough.
- Third-party frontend libraries that are part of the long-term project standard should be self-hosted under `wwwroot/lib` or `wwwroot/vendor` instead of being loaded from public CDNs.
- Remote embeds that are service-bound, such as reCAPTCHA, Tawk, hosted video, or external social URLs, may stay remote when the integration itself requires a live third-party endpoint.
- When cleaning template leftovers, only delete sample, test, demo, backup, modal, or legacy files after a reference scan confirms they are not used by current views, controllers, scripts, or partial rendering paths.

## Routing And Error Handling

- Client and admin errors should resolve to their own contexts.
- Admin invalid routes should return admin error views, not client error views.
- Keep middleware changes in `Program.cs` aligned with area behavior.

## Product Metadata Rules

- Product specs and variant attributes are managed from dedicated admin settings pages.
- Do not reintroduce unmanaged free-form spec editing inside unrelated views.
- Specs and attributes should preserve:
  - generated code conventions
  - sort order conventions
  - active/inactive state
  - reusable management pages under admin settings

## When Adding A New Page

- Check whether an existing shared layout, partial, CSS namespace, or pagination pattern already exists.
- Add CSS to shared site CSS files unless the style is truly unique and isolated.
- Keep the page aligned with:
  - typography
  - spacing
  - panel shape
  - button style
  - form behavior
  - pagination behavior
- If the page is admin:
  - favor grouped panels, tables, manual pagination, compact controls
- If the page is client:
  - favor responsive product-first layout and lightweight interactions

## Anti-Patterns To Avoid

- one page = one font
- one page = one pagination style
- one page = one table system
- large inline `<style>` or `<script>` blocks inside Razor views when the behavior is reusable
- putting business logic into views
- mixing admin and client visual language
- using DataTables for new pages without a strong reason
- duplicating modal logic across pages when one reusable flow is possible

## Maintenance Workflow

Before changing a page:

1. Read `PROJECT_MAP.md` for module location and ownership context.
2. Read this file for consistency rules.
3. Check the nearest shared layout, shared CSS, and related controller/service pattern.
4. Extend existing patterns before creating new ones.

After changing a page:

1. Confirm typography and spacing still match the rest of the site.
2. Confirm responsive behavior on mobile and desktop.
3. Confirm no new inline styling system was introduced.
4. Confirm pagination/table/form behavior remains consistent with admin or client standards.

## Suggested Living Updates

Update this file whenever one of these changes:

- a new cross-site UI pattern is introduced
- a new shared admin/client component becomes standard
- pagination, typography, or form conventions change
- metadata management rules change
- a new architectural boundary becomes important for maintainability
