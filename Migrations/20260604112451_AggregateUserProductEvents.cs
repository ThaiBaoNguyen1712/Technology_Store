using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tech_Store.Migrations
{
    /// <inheritdoc />
    public partial class AggregateUserProductEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProductEvent_Aggregated",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    view_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    add_to_cart_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    wishlist_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    purchase_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    interaction_score = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    last_interacted_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductEvent_Aggregated", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserProductEvent_Aggregated_Product",
                        column: x => x.product_id,
                        principalTable: "Product",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductEvent_Aggregated_User",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO UserProductEvent_Aggregated
                (
                    user_id,
                    session_id,
                    product_id,
                    view_count,
                    add_to_cart_count,
                    wishlist_count,
                    purchase_count,
                    interaction_score,
                    last_interacted_at
                )
                SELECT
                    user_id,
                    session_id,
                    product_id,
                    SUM(CASE
                        WHEN event_type IN ('view_detail', 'search_click', 'homepage_click', 'recommendation_click') THEN 1
                        ELSE 0
                    END) AS view_count,
                    CASE
                        WHEN SUM(CASE
                            WHEN event_type = 'add_cart' THEN 1
                            WHEN event_type = 'remove_cart' THEN -1
                            ELSE 0
                        END) < 0 THEN 0
                        ELSE SUM(CASE
                            WHEN event_type = 'add_cart' THEN 1
                            WHEN event_type = 'remove_cart' THEN -1
                            ELSE 0
                        END)
                    END AS add_to_cart_count,
                    CASE
                        WHEN SUM(CASE
                            WHEN event_type = 'wishlist_add' THEN 1
                            WHEN event_type = 'wishlist_remove' THEN -1
                            ELSE 0
                        END) < 0 THEN 0
                        ELSE SUM(CASE
                            WHEN event_type = 'wishlist_add' THEN 1
                            WHEN event_type = 'wishlist_remove' THEN -1
                            ELSE 0
                        END)
                    END AS wishlist_count,
                    SUM(CASE
                        WHEN event_type = 'purchase' THEN 1
                        ELSE 0
                    END) AS purchase_count,
                    SUM(weight) AS interaction_score,
                    MAX(created_at) AS last_interacted_at
                FROM UserProductEvent
                GROUP BY user_id, session_id, product_id;
                """);

            migrationBuilder.DropTable(
                name: "UserProductEvent");

            migrationBuilder.RenameTable(
                name: "UserProductEvent_Aggregated",
                newName: "UserProductEvent");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_LastInteractedAt",
                table: "UserProductEvent",
                column: "last_interacted_at");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_ProductId_LastInteractedAt",
                table: "UserProductEvent",
                columns: new[] { "product_id", "last_interacted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_SessionId_ProductId",
                table: "UserProductEvent",
                columns: new[] { "session_id", "product_id" },
                unique: true,
                filter: "[session_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_UserId_ProductId",
                table: "UserProductEvent",
                columns: new[] { "user_id", "product_id" },
                unique: true,
                filter: "[user_id] IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProductEvent_LastInteractedAt",
                table: "UserProductEvent");

            migrationBuilder.DropIndex(
                name: "IX_UserProductEvent_ProductId_LastInteractedAt",
                table: "UserProductEvent");

            migrationBuilder.DropIndex(
                name: "IX_UserProductEvent_SessionId_ProductId",
                table: "UserProductEvent");

            migrationBuilder.DropIndex(
                name: "IX_UserProductEvent_UserId_ProductId",
                table: "UserProductEvent");

            migrationBuilder.CreateTable(
                name: "UserProductEvent_Raw",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    weight = table.Column<double>(type: "float", nullable: false),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductEvent_Raw", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserProductEvent_Raw_Product",
                        column: x => x.product_id,
                        principalTable: "Product",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductEvent_Raw_User",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO UserProductEvent_Raw
                (
                    user_id,
                    session_id,
                    product_id,
                    event_type,
                    weight,
                    source,
                    metadata_json,
                    created_at
                )
                SELECT
                    user_id,
                    session_id,
                    product_id,
                    'view_detail',
                    interaction_score,
                    'aggregate_migration_down',
                    NULL,
                    last_interacted_at
                FROM UserProductEvent;
                """);

            migrationBuilder.DropTable(
                name: "UserProductEvent");

            migrationBuilder.RenameTable(
                name: "UserProductEvent_Raw",
                newName: "UserProductEvent");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_EventType_CreatedAt",
                table: "UserProductEvent",
                columns: new[] { "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_ProductId_CreatedAt",
                table: "UserProductEvent",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_SessionId_CreatedAt",
                table: "UserProductEvent",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvent_UserId_CreatedAt",
                table: "UserProductEvent",
                columns: new[] { "user_id", "created_at" });

        }
    }
}
