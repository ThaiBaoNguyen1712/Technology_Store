using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSePayCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingSePayCheckout",
                columns: table => new
                {
                    pending_sepay_checkout_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(20,2)", nullable: false),
                    payment_content = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    checkout_payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    payment_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    order_status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    order_id = table.Column<int>(type: "int", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    gateway_payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSePayCheckout", x => x.pending_sepay_checkout_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSePayCheckout_order_id",
                table: "PendingSePayCheckout",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSePayCheckout_payment_content",
                table: "PendingSePayCheckout",
                column: "payment_content",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingSePayCheckout_user_id",
                table: "PendingSePayCheckout",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingSePayCheckout");
        }
    }
}
