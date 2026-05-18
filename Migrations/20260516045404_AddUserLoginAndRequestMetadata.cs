using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLoginAndRequestMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_login_device",
                table: "User",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_login_ip",
                table: "User",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_request_at",
                table: "User",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_request_device",
                table: "User",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_request_ip",
                table: "User",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_login_device",
                table: "User");

            migrationBuilder.DropColumn(
                name: "last_login_ip",
                table: "User");

            migrationBuilder.DropColumn(
                name: "last_request_at",
                table: "User");

            migrationBuilder.DropColumn(
                name: "last_request_device",
                table: "User");

            migrationBuilder.DropColumn(
                name: "last_request_ip",
                table: "User");
        }
    }
}
