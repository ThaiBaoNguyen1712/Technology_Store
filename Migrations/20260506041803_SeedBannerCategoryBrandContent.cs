using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class SeedBannerCategoryBrandContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "banner_id", "alt_text", "desktop_image_url", "is_active", "is_deleted", "mobile_image_url", "name", "notes" },
                values: new object[,]
                {
                    { 1101, "Banner điện thoại nổi bật", "https://images.macrumors.com/t/H0wescJV-Z32v37P2JxpOBsFX8c=/2500x0/filters:no_upscale()/article-new/2024/11/iPhone-17-Pro-Dual-Tone-Rectangle-Feature-1.jpg", true, false, null, "Category hero điện thoại", null },
                    { 1102, "Banner laptop hiệu năng cao", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=1600&q=80", true, false, null, "Category hero laptop", null },
                    { 1103, "Banner máy tính bảng học tập và giải trí", "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=1600&q=80", true, false, null, "Category hero tablet", null },
                    { 1104, "Banner phụ kiện công nghệ", "https://images.unsplash.com/photo-1585338447937-7082f8fc763d?auto=format&fit=crop&w=1600&q=80", true, false, null, "Category hero phụ kiện", null },
                    { 1105, "Banner Apple", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=1600&q=80", true, false, null, "Brand hero Apple", null },
                    { 1106, "Banner Samsung", "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?auto=format&fit=crop&w=1600&q=80", true, false, null, "Brand hero Samsung", null },
                    { 1107, "Banner ASUS", "https://images.unsplash.com/photo-1593642702744-d377ab507dc8?auto=format&fit=crop&w=1600&q=80", true, false, null, "Brand hero ASUS", null },
                    { 1108, "Banner Lenovo", "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?auto=format&fit=crop&w=1600&q=80", true, false, null, "Brand hero Lenovo", null }
                });

            migrationBuilder.InsertData(
                table: "BannerPositionMaps",
                columns: new[] { "banner_position_map_id", "banner_id", "banner_position_id", "display_category_id", "end_at", "is_active", "is_default", "priority", "start_at" },
                values: new object[,]
                {
                    { 3101, 1101, 3, 1, null, true, false, 500, null },
                    { 3102, 1102, 3, 2, null, true, false, 500, null },
                    { 3103, 1103, 3, 3, null, true, false, 500, null },
                    { 3104, 1104, 3, 5, null, true, false, 500, null },
                    { 3105, 1105, 4, null, null, true, false, 500, null },
                    { 3106, 1106, 4, 1, null, true, false, 500, null },
                    { 3107, 1107, 4, 2, null, true, false, 500, null },
                    { 3108, 1108, 4, null, null, true, false, 500, null }
                });

            migrationBuilder.InsertData(
                table: "BannerTargets",
                columns: new[] { "banner_target_id", "banner_id", "brand_id", "category_id", "external_url", "open_in_new_tab", "product_id", "target_type" },
                values: new object[,]
                {
                    { 2101, 1101, null, 1, null, false, null, "category" },
                    { 2102, 1102, null, 2, null, false, null, "category" },
                    { 2103, 1103, null, 3, null, false, null, "category" },
                    { 2104, 1104, null, 5, null, false, null, "category" },
                    { 2105, 1105, 4, null, null, false, null, "brand" },
                    { 2106, 1106, 2, null, null, false, null, "brand" },
                    { 2107, 1107, 354036, null, null, false, null, "brand" },
                    { 2108, 1108, 9, null, null, false, null, "brand" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3101);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3102);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3103);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3104);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3105);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3106);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3107);

            migrationBuilder.DeleteData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3108);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2101);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2102);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2103);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2104);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2105);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2106);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2107);

            migrationBuilder.DeleteData(
                table: "BannerTargets",
                keyColumn: "banner_target_id",
                keyValue: 2108);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1101);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1102);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1103);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1104);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1105);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1106);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1107);

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "banner_id",
                keyValue: 1108);
        }
    }
}
