using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sortOrder",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Product_sortOrder",
                table: "Product",
                column: "sortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_sortOrder",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "sortOrder",
                table: "Product");
        }
    }
}
