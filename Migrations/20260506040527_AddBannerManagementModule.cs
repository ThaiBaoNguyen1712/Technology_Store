using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Banner",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "device",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "imageUrl",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "link",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "position",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "refId",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "sortOrder",
                table: "Banner");

            migrationBuilder.RenameTable(
                name: "Banner",
                newName: "Banners");

            migrationBuilder.RenameColumn(
                name: "active",
                table: "Banners",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Banners",
                newName: "notes");

            migrationBuilder.AlterColumn<int>(
                name: "banner_id",
                table: "Banners",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "0, 1");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "Banners",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alt_text",
                table: "Banners",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "desktop_image_url",
                table: "Banners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Banners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "mobile_image_url",
                table: "Banners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "Banners",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Banners",
                table: "Banners",
                column: "banner_id");

            migrationBuilder.CreateTable(
                name: "BannerPositions",
                columns: table => new
                {
                    banner_position_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerPositions", x => x.banner_position_id);
                });

            migrationBuilder.CreateTable(
                name: "BannerTargets",
                columns: table => new
                {
                    banner_target_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    banner_id = table.Column<int>(type: "int", nullable: false),
                    target_type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    brand_id = table.Column<int>(type: "int", nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: true),
                    external_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    open_in_new_tab = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerTargets", x => x.banner_target_id);
                    table.ForeignKey(
                        name: "FK_BannerTargets_Banners",
                        column: x => x.banner_id,
                        principalTable: "Banners",
                        principalColumn: "banner_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BannerTargets_Brands",
                        column: x => x.brand_id,
                        principalTable: "Brand",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BannerTargets_Categories",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BannerTargets_Products",
                        column: x => x.product_id,
                        principalTable: "Product",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BannerPositionMaps",
                columns: table => new
                {
                    banner_position_map_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    banner_id = table.Column<int>(type: "int", nullable: false),
                    banner_position_id = table.Column<int>(type: "int", nullable: false),
                    display_category_id = table.Column<int>(type: "int", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    start_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    end_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerPositionMaps", x => x.banner_position_map_id);
                    table.ForeignKey(
                        name: "FK_BannerPositionMaps_BannerPositions",
                        column: x => x.banner_position_id,
                        principalTable: "BannerPositions",
                        principalColumn: "banner_position_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BannerPositionMaps_Banners",
                        column: x => x.banner_id,
                        principalTable: "Banners",
                        principalColumn: "banner_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BannerPositionMaps_Categories",
                        column: x => x.display_category_id,
                        principalTable: "Category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "BannerPositions",
                columns: new[] { "banner_position_id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "home-hero-main", "Carousel lớn giữa trang chủ", true, "Hero chính trang chủ" },
                    { 2, "home-hero-promo", "Hai banner phụ bên phải hero trang chủ", true, "Promo phụ trang chủ" },
                    { 3, "category-hero", "Banner đầu trang danh mục", true, "Hero danh mục" },
                    { 4, "brand-hero", "Banner đầu trang thương hiệu", true, "Hero thương hiệu" }
                });

            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "banner_id", "alt_text", "desktop_image_url", "is_active", "is_deleted", "mobile_image_url", "name", "notes" },
                values: new object[,]
                {
                    { 1001, "Khuyến mãi điện thoại nổi bật", "https://images.macrumors.com/t/H0wescJV-Z32v37P2JxpOBsFX8c=/2500x0/filters:no_upscale()/article-new/2024/11/iPhone-17-Pro-Dual-Tone-Rectangle-Feature-1.jpg", true, false, null, "Banner mặc định hero 1", null },
                    { 1002, "Khuyến mãi Samsung nổi bật", "https://i.gadgets360cdn.com/large/samsung_galaxy_s25_ultra_technizoconcept_inline_1731133022562.jpg", true, false, null, "Banner mặc định hero 2", null },
                    { 1003, "Khuyến mãi flagship mới", "https://cdn.mos.cms.futurecdn.net/hyKSYAeHLcrRtExozn7EBA-1200-80.jpg", true, false, null, "Banner mặc định hero 3", null },
                    { 1004, "Khuyến mãi iPhone", "/Client/asset/img/R.png", true, false, null, "Banner mặc định promo 1", null },
                    { 1005, "Khuyến mãi Samsung", "/Client/asset/img/right-banner-14-10.webp", true, false, null, "Banner mặc định promo 2", null }
                });

            migrationBuilder.InsertData(
                table: "BannerPositionMaps",
                columns: new[] { "banner_position_map_id", "banner_id", "banner_position_id", "display_category_id", "end_at", "is_active", "is_default", "priority", "start_at" },
                values: new object[,]
                {
                    { 3001, 1001, 1, null, null, true, true, 300, null },
                    { 3002, 1002, 1, null, null, true, true, 200, null },
                    { 3003, 1003, 1, null, null, true, true, 100, null },
                    { 3004, 1004, 2, null, null, true, true, 200, null },
                    { 3005, 1005, 2, null, null, true, true, 100, null },
                    { 3006, 1001, 3, null, null, true, true, 100, null },
                    { 3007, 1002, 4, null, null, true, true, 100, null }
                });

            migrationBuilder.InsertData(
                table: "BannerTargets",
                columns: new[] { "banner_target_id", "banner_id", "brand_id", "category_id", "external_url", "open_in_new_tab", "product_id", "target_type" },
                values: new object[,]
                {
                    { 2001, 1001, null, null, "/Search?q=iphone", false, null, "url" },
                    { 2002, 1002, null, null, "/Search?q=samsung", false, null, "url" },
                    { 2003, 1003, null, null, "/Search?q=flagship", false, null, "url" },
                    { 2004, 1004, null, null, "/Search?q=iphone", false, null, "url" },
                    { 2005, 1005, null, null, "/Search?q=samsung", false, null, "url" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banners_is_deleted_is_active",
                table: "Banners",
                columns: new[] { "is_deleted", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_banner_id",
                table: "BannerPositionMaps",
                column: "banner_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_display_category_id",
                table: "BannerPositionMaps",
                column: "display_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_PositionScopePriority",
                table: "BannerPositionMaps",
                columns: new[] { "banner_position_id", "display_category_id", "is_active", "is_default", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositions_code",
                table: "BannerPositions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BannerTargets_banner_id",
                table: "BannerTargets",
                column: "banner_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BannerTargets_brand_id",
                table: "BannerTargets",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerTargets_category_id",
                table: "BannerTargets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerTargets_product_id",
                table: "BannerTargets",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerTargets_TargetLookup",
                table: "BannerTargets",
                columns: new[] { "target_type", "category_id", "brand_id", "product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerPositionMaps");

            migrationBuilder.DropTable(
                name: "BannerTargets");

            migrationBuilder.DropTable(
                name: "BannerPositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Banners",
                table: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_Banners_is_deleted_is_active",
                table: "Banners");

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1005);

            migrationBuilder.DropColumn(
                name: "alt_text",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "desktop_image_url",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "mobile_image_url",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "name",
                table: "Banners");

            migrationBuilder.RenameTable(
                name: "Banners",
                newName: "Banner");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Banner",
                newName: "active");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Banner",
                newName: "type");

            migrationBuilder.AlterColumn<int>(
                name: "banner_id",
                table: "Banner",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "0, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<bool>(
                name: "active",
                table: "Banner",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "device",
                table: "Banner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imageUrl",
                table: "Banner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link",
                table: "Banner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "position",
                table: "Banner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refId",
                table: "Banner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sortOrder",
                table: "Banner",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Banner",
                table: "Banner",
                column: "banner_id");
        }
    }
}
