using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260224103000_ForceIndividualCustomerType")]
    public partial class ForceIndividualCustomerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [Customers]
                SET [Type] = 1
                WHERE [Type] <> 1 OR [Type] IS NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Customers_Type_IndividualOnly'
                )
                BEGIN
                    ALTER TABLE [Customers]
                    ADD CONSTRAINT [CK_Customers_Type_IndividualOnly]
                    CHECK ([Type] = 1);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE name = 'CK_Customers_Type_IndividualOnly'
                )
                BEGIN
                    ALTER TABLE [Customers]
                    DROP CONSTRAINT [CK_Customers_Type_IndividualOnly];
                END
            ");
        }
    }
}
