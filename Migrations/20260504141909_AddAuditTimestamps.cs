using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Voucher",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Voucher",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "VarientProduct",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "VariantAttribute",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "VariantAttribute",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "User",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "SpecValue",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "SpecValue",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Specs",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Specs",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Setting",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Setting",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Review",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Review",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Payment",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Payment",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "OrderItem",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "OrderItem",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Order",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Order",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "InventoryTransactionsDetail",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "InventoryTransactionsDetail",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "InventoryTransactions",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Gallery",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Gallery",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Category",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Category",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "CartItem",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "CartItem",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Brand",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Brand",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Banner",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Banner",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "AttributeValue",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "AttributeValue",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Attribute",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Attribute",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Address",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Address",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "VarientProduct");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "VariantAttribute");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "VariantAttribute");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "User");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "SpecValue");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "SpecValue");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Specs");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Setting");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Setting");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Review");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "InventoryTransactionsDetail");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "InventoryTransactionsDetail");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Gallery");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Gallery");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Banner");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "AttributeValue");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "AttributeValue");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Attribute");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Attribute");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Address");
        }
    }
}
