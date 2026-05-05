using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandCategoryMappingsAndSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "Brand",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BrandCategory",
                columns: table => new
                {
                    brand_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandCategory", x => new { x.brand_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_BrandCategory_Brand",
                        column: x => x.brand_id,
                        principalTable: "Brand",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BrandCategory_Category",
                        column: x => x.category_id,
                        principalTable: "Category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandCategory_category_id",
                table: "BrandCategory",
                column: "category_id");

            migrationBuilder.Sql(@"
                INSERT INTO BrandCategory (brand_id, category_id)
                SELECT DISTINCT BrandId, category_id
                FROM Brand
                WHERE category_id IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM BrandCategory bc
                      WHERE bc.brand_id = Brand.BrandId
                        AND bc.category_id = Brand.category_id
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandCategory");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "Brand");
        }
    }
}
