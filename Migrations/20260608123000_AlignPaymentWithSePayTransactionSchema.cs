using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Tech_Store.Models.ApplicationDbContext))]
    [Migration("20260608123000_AlignPaymentWithSePayTransactionSchema")]
    public partial class AlignPaymentWithSePayTransactionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "payment_id",
                table: "Payment",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "payment_method",
                table: "Payment",
                newName: "gateway");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "Payment",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Payment",
                newName: "amount_in");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Payment",
                newName: "payment_status");

            migrationBuilder.AlterColumn<string>(
                name: "gateway",
                table: "Payment",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_in",
                table: "Payment",
                type: "decimal(20,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18 ,2)");

            migrationBuilder.AddColumn<string>(
                name: "account_number",
                table: "Payment",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "accumulated",
                table: "Payment",
                type: "decimal(20,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_out",
                table: "Payment",
                type: "decimal(20,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "body",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "Payment",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "Payment",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_account",
                table: "Payment",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transaction_content",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account_number",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "accumulated",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "amount_out",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "body",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "code",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "sub_account",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "transaction_content",
                table: "Payment");

            migrationBuilder.AlterColumn<string>(
                name: "gateway",
                table: "Payment",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_in",
                table: "Payment",
                type: "decimal(18 ,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,2)");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Payment",
                newName: "payment_id");

            migrationBuilder.RenameColumn(
                name: "gateway",
                table: "Payment",
                newName: "payment_method");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "Payment",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "amount_in",
                table: "Payment",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                table: "Payment",
                newName: "status");
        }
    }
}
