using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecsValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Specs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "Specs",
                type: "bit",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "Product",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "100000, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateTable(
                name: "SpecValue",
                columns: table => new
                {
                    specValue_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecValue", x => x.specValue_id);
                    table.ForeignKey(
                        name: "FK_SpecValue_Product",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "FK_SpecValue_Specs",
                        column: x => x.SpecId,
                        principalTable: "Specs",
                        principalColumn: "spec_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecValue_ProductId",
                table: "SpecValue",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecValue_SpecId",
                table: "SpecValue",
                column: "SpecId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecValue");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "Specs");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Specs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "Product",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "100000, 1");
        }
    }
}
