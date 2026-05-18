using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersToInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplierId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    contact_name = table.Column<string>(type: "nvarchar(155)", maxLength: 155, nullable: true),
                    phone_number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "varchar(155)", unicode: false, maxLength: 155, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.supplier_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_supplierId",
                table: "InventoryTransactions",
                column: "supplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_code",
                table: "Supplier",
                column: "code",
                unique: true);

            migrationBuilder.InsertData(
                table: "Supplier",
                columns: new[] { "supplier_id", "code", "name", "contact_name", "phone_number", "email", "address", "notes", "is_active" },
                values: new object[,]
                {
                    { 1, "NCC-APPLE", "Công ty TNHH Phân phối Apple Việt", "Phòng mua hàng Apple", "02873008811", "ncc.apple@techstore.vn", "Quận 1, TP. Hồ Chí Minh", "Nhà cung cấp mẫu cho nhóm điện thoại và phụ kiện Apple.", true },
                    { 2, "NCC-SAMSUNG", "Công ty CP Điện tử Samsung Hub", "Bộ phận kênh MT", "02873009922", "ncc.samsung@techstore.vn", "Thành phố Thủ Đức, TP. Hồ Chí Minh", "Nhà cung cấp mẫu cho điện thoại, tablet và thiết bị gia dụng Samsung.", true },
                    { 3, "NCC-PHUKIEN", "Công ty TNHH Phụ kiện Số Miền Nam", "Tổ điều phối kho", "02873007733", "ncc.phukien@techstore.vn", "Quận 12, TP. Hồ Chí Minh", "Nhà cung cấp mẫu cho cáp, sạc, tai nghe và phụ kiện tiêu chuẩn ecommerce.", true }
                });

            migrationBuilder.Sql(
                """
                ;WITH ImportTransactions AS (
                    SELECT
                        inventory_transactions_id,
                        ROW_NUMBER() OVER (ORDER BY inventory_transactions_id) AS row_num
                    FROM InventoryTransactions
                    WHERE LOWER([type]) = 'import'
                )
                UPDATE inventory
                SET supplierId = CASE
                    WHEN import_rows.row_num % 3 = 1 THEN 1
                    WHEN import_rows.row_num % 3 = 2 THEN 2
                    ELSE 3
                END
                FROM InventoryTransactions AS inventory
                INNER JOIN ImportTransactions AS import_rows
                    ON inventory.inventory_transactions_id = import_rows.inventory_transactions_id;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Supplier",
                table: "InventoryTransactions",
                column: "supplierId",
                principalTable: "Supplier",
                principalColumn: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Supplier",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_supplierId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "supplierId",
                table: "InventoryTransactions");
        }
    }
}
