using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class FixProductIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ__VarientP__CA1ECF0D1E3D1C2E",
                table: "VarientProduct",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_sellPrice",
                table: "Product",
                column: "sellPrice");

            migrationBuilder.CreateIndex(
                name: "IX_Product_sku",
                table: "Product",
                column: "sku",
                unique: true,
                filter: "[sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Product_slug",
                table: "Product",
                column: "slug",
                unique: true,
                filter: "[slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ__VarientP__CA1ECF0D1E3D1C2E",
                table: "VarientProduct");

            migrationBuilder.DropIndex(
                name: "IX_Product_sellPrice",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_sku",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_slug",
                table: "Product");
        }
    }
}
