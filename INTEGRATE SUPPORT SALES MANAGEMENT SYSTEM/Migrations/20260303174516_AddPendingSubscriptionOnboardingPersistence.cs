using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSubscriptionOnboardingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingSubscriptionOnboardings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AdminFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdminLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SubscriptionPlanId = table.Column<int>(type: "int", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InitialBranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    CheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CheckoutStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSubscriptionOnboardings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSubscriptionOnboardings_AdminEmail",
                table: "PendingSubscriptionOnboardings",
                column: "AdminEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSubscriptionOnboardings_CheckoutSessionId",
                table: "PendingSubscriptionOnboardings",
                column: "CheckoutSessionId",
                unique: true,
                filter: "[CheckoutSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSubscriptionOnboardings_IsCompleted",
                table: "PendingSubscriptionOnboardings",
                column: "IsCompleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingSubscriptionOnboardings");
        }
    }
}
