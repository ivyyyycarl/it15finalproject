using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceReportAndRenameAgentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductId1",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_ProductId1",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "OrderDetails");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PerformanceReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    TicketsResolved = table.Column<int>(type: "int", nullable: false),
                    AvgHandlingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SalesConversionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceReports_Users_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImageUrl", "IsActive", "IsSubscription", "MinStockLevel", "Name", "Price", "SKU", "StockQuantity", "SubscriptionMonths", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 6, new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(1029), "Premium 100% cotton t-shirt in various colors", null, true, false, 5, "Classic Cotton T-Shirt", 29.99m, "APP-001", 500, null, null },
                    { 2, 6, new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(1032), "Comfortable slim fit denim jeans", null, true, false, 5, "Slim Fit Jeans", 59.99m, "APP-002", 200, null, null },
                    { 3, 6, new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(1035), "Insulated winter jacket for cold weather", null, true, false, 5, "Winter Parka Jacket", 129.99m, "APP-003", 50, null, null },
                    { 4, 6, new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(1038), "Soft fleece hoodie with kangaroo pocket", null, true, false, 5, "Pullover Hoodie", 49.99m, "APP-004", 150, null, null },
                    { 5, 6, new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(1041), "Versatile chino pants for work or casual wear", null, true, false, 5, "Casual Chinos", 54.99m, "APP-005", 100, null, null }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 22, 25, 13, DateTimeKind.Utc).AddTicks(579));

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReports_AgentId",
                table: "PerformanceReports",
                column: "AgentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceReports");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "OrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 13, 21, 7, 604, DateTimeKind.Utc).AddTicks(9364));

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ProductId1",
                table: "OrderDetails",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Products_ProductId1",
                table: "OrderDetails",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
