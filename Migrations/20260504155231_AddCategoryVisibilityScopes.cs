using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryVisibilityScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "visible_on_category_page",
                table: "Category",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "visible_on_other_pages",
                table: "Category",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE Category
                SET visible_on_category_page = 1,
                    visible_on_other_pages = 1
                WHERE visible_on_category_page IS NULL
                   OR visible_on_other_pages IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "visible_on_category_page",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "visible_on_other_pages",
                table: "Category");
        }
    }
}
