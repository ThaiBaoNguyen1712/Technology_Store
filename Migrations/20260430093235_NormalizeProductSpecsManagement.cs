using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeProductSpecsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpecValue_ProductId",
                table: "SpecValue");

            migrationBuilder.RenameColumn(
                name: "SpecId",
                table: "SpecValue",
                newName: "spec_id");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "SpecValue",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "IX_SpecValue_SpecId",
                table: "SpecValue",
                newName: "IX_SpecValue_spec_id");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "SpecValue",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "SpecValue",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Specs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "Specs",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "Specs",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "group_name",
                table: "Specs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_type",
                table: "Specs",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "text");

            migrationBuilder.AddColumn<bool>(
                name: "is_filterable",
                table: "Specs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible_on_product_page",
                table: "Specs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "Specs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "Specs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [Specs]
                SET [code] = CONCAT('spec_', [spec_id])
                WHERE [code] = '' OR [code] IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE [Specs]
                SET [input_type] = 'text'
                WHERE [input_type] = '' OR [input_type] IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE [Specs]
                SET [is_visible_on_product_page] = 1
                WHERE [is_visible_on_product_page] = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_SpecValue_product_id_spec_id",
                table: "SpecValue",
                columns: new[] { "product_id", "spec_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specs_code",
                table: "Specs",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpecValue_product_id_spec_id",
                table: "SpecValue");

            migrationBuilder.DropIndex(
                name: "IX_Specs_code",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "SpecValue");

            migrationBuilder.DropColumn(
                name: "code",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "group_name",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "input_type",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "is_filterable",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "is_visible_on_product_page",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "unit",
                table: "Specs");

            migrationBuilder.RenameColumn(
                name: "spec_id",
                table: "SpecValue",
                newName: "SpecId");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "SpecValue",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_SpecValue_spec_id",
                table: "SpecValue",
                newName: "IX_SpecValue_SpecId");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "SpecValue",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Specs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "Specs",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_SpecValue_ProductId",
                table: "SpecValue",
                column: "ProductId");
        }
    }
}
