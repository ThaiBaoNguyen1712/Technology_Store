using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerBrandScopeMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BannerPositionMaps_PositionScopePriority",
                table: "BannerPositionMaps");

            migrationBuilder.AddColumn<int>(
                name: "display_brand_id",
                table: "BannerPositionMaps",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3001,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3002,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3003,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3004,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3005,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3006,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3007,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3101,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3102,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3103,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3104,
                column: "display_brand_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3105,
                column: "display_brand_id",
                value: 4);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3106,
                columns: new[] { "display_brand_id", "display_category_id" },
                values: new object[] { 2, null });

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3107,
                columns: new[] { "display_brand_id", "display_category_id" },
                values: new object[] { 354036, null });

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3108,
                column: "display_brand_id",
                value: 9);

            migrationBuilder.Sql(@"
                UPDATE bpm
                SET
                    bpm.display_brand_id = bt.brand_id,
                    bpm.display_category_id = NULL
                FROM BannerPositionMaps bpm
                INNER JOIN BannerPositions bp ON bp.banner_position_id = bpm.banner_position_id
                INNER JOIN BannerTargets bt ON bt.banner_id = bpm.banner_id
                WHERE bp.code = 'brand-hero'
                    AND bt.brand_id IS NOT NULL
                    AND (bpm.display_brand_id IS NULL OR bpm.display_brand_id <> bt.brand_id);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_display_brand_id",
                table: "BannerPositionMaps",
                column: "display_brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_PositionScopePriority",
                table: "BannerPositionMaps",
                columns: new[] { "banner_position_id", "display_category_id", "display_brand_id", "is_active", "is_default", "priority" });

            migrationBuilder.AddForeignKey(
                name: "FK_BannerPositionMaps_Brands",
                table: "BannerPositionMaps",
                column: "display_brand_id",
                principalTable: "Brand",
                principalColumn: "BrandId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BannerPositionMaps_Brands",
                table: "BannerPositionMaps");

            migrationBuilder.DropIndex(
                name: "IX_BannerPositionMaps_display_brand_id",
                table: "BannerPositionMaps");

            migrationBuilder.DropIndex(
                name: "IX_BannerPositionMaps_PositionScopePriority",
                table: "BannerPositionMaps");

            migrationBuilder.DropColumn(
                name: "display_brand_id",
                table: "BannerPositionMaps");

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3106,
                column: "display_category_id",
                value: 1);

            migrationBuilder.UpdateData(
                table: "BannerPositionMaps",
                keyColumn: "banner_position_map_id",
                keyValue: 3107,
                column: "display_category_id",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_BannerPositionMaps_PositionScopePriority",
                table: "BannerPositionMaps",
                columns: new[] { "banner_position_id", "display_category_id", "is_active", "is_default", "priority" });
        }
    }
}
