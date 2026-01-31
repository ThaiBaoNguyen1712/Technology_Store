using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class RemaneHistoryPRDToInventoryTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductHistoryDetail");

            migrationBuilder.DropTable(
                name: "ProductHistory");

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    inventory_transactions_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    productId = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    userId = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.inventory_transactions_id);
                    table.ForeignKey(
                        name: "FK_ProductHistory_Product",
                        column: x => x.productId,
                        principalTable: "Product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "FK_ProductHistory_User",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactionsDetail",
                columns: table => new
                {
                    inventoryTrans_detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    historyId = table.Column<int>(type: "int", nullable: false),
                    varientId = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactionsDetail", x => x.inventoryTrans_detail_id);
                    table.ForeignKey(
                        name: "FK_ProductHistoryDetail_ProductHistory",
                        column: x => x.historyId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "inventory_transactions_id");
                    table.ForeignKey(
                        name: "FK_ProductHistoryDetail_VarientProduct",
                        column: x => x.varientId,
                        principalTable: "VarientProduct",
                        principalColumn: "varientId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_productId",
                table: "InventoryTransactions",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_userId",
                table: "InventoryTransactions",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionsDetail_historyId",
                table: "InventoryTransactionsDetail",
                column: "historyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionsDetail_varientId",
                table: "InventoryTransactionsDetail",
                column: "varientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactionsDetail");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.CreateTable(
                name: "ProductHistory",
                columns: table => new
                {
                    product_historyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    productId = table.Column<int>(type: "int", nullable: false),
                    userId = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductHistory", x => x.product_historyId);
                    table.ForeignKey(
                        name: "FK_ProductHistory_Product",
                        column: x => x.productId,
                        principalTable: "Product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "FK_ProductHistory_User",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "ProductHistoryDetail",
                columns: table => new
                {
                    historyDetail_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    historyId = table.Column<int>(type: "int", nullable: false),
                    varientId = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductHistoryDetail", x => x.historyDetail_Id);
                    table.ForeignKey(
                        name: "FK_ProductHistoryDetail_ProductHistory",
                        column: x => x.historyId,
                        principalTable: "ProductHistory",
                        principalColumn: "product_historyId");
                    table.ForeignKey(
                        name: "FK_ProductHistoryDetail_VarientProduct",
                        column: x => x.varientId,
                        principalTable: "VarientProduct",
                        principalColumn: "varientId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductHistory_productId",
                table: "ProductHistory",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHistory_userId",
                table: "ProductHistory",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHistoryDetail_historyId",
                table: "ProductHistoryDetail",
                column: "historyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductHistoryDetail_varientId",
                table: "ProductHistoryDetail",
                column: "varientId");
        }
    }
}
