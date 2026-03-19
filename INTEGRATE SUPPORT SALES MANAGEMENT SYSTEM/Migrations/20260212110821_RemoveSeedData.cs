using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImageUrl", "IsActive", "IsSubscription", "MinStockLevel", "Name", "Price", "SKU", "StockQuantity", "SubscriptionMonths", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 6, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1806), "Premium 100% cotton t-shirt in various colors", null, true, false, 5, "Classic Cotton T-Shirt", 29.99m, "APP-001", 500, null, null },
                    { 2, 6, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1810), "Comfortable slim fit denim jeans", null, true, false, 5, "Slim Fit Jeans", 59.99m, "APP-002", 200, null, null },
                    { 3, 6, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1813), "Insulated winter jacket for cold weather", null, true, false, 5, "Winter Parka Jacket", 129.99m, "APP-003", 50, null, null },
                    { 4, 6, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1816), "Soft fleece hoodie with kangaroo pocket", null, true, false, 5, "Pullover Hoodie", 49.99m, "APP-004", 150, null, null },
                    { 5, 6, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1819), "Versatile chino pants for work or casual wear", null, true, false, 5, "Casual Chinos", 54.99m, "APP-005", 100, null, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "Phone", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1528), "admin@example.com", "Admin", true, null, "User", "$2a$11$V1nE8ud6nd6fpVrHvENU1u5D92QVcqWNYqlUivFPViFVqfGP81Xp.", "0000000000", 3, null },
                    { 2, new DateTime(2026, 2, 10, 13, 38, 25, 157, DateTimeKind.Utc).AddTicks(1532), "ivycarlb@gmail.com", "Super", true, null, "Admin", "$2a$11$V1nE8ud6nd6fpVrHvENU1u5D92QVcqWNYqlUivFPViFVqfGP81Xp.", "9999999999", 4, null }
                });
        }
    }
}
