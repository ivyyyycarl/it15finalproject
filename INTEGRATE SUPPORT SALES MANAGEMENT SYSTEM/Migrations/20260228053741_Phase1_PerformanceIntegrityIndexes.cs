using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_PerformanceIntegrityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Category",
                table: "Products",
                columns: new[] { "IsActive", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StockQuantity",
                table: "Products",
                column: "StockQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StockQuantity_MinStockLevel",
                table: "Products",
                columns: new[] { "StockQuantity", "MinStockLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AgentId_OrderDate",
                table: "Orders",
                columns: new[] { "AgentId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_OrderDate",
                table: "Orders",
                columns: new[] { "CustomerId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus",
                table: "Orders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatus_OrderDate",
                table: "Orders",
                columns: new[] { "PaymentStatus", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_OrderDate",
                table: "Orders",
                columns: new[] { "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Company",
                table: "Customers",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_LastName",
                table: "Customers",
                column: "LastName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StockQuantity",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StockQuantity_MinStockLevel",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AgentId_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatus",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatus_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Company",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_LastName",
                table: "Customers");
        }
    }
}
