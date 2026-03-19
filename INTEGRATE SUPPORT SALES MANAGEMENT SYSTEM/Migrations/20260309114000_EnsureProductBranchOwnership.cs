using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Migrations
{
    public partial class EnsureProductBranchOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Products','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Products','BranchId') IS NULL
        ALTER TABLE dbo.Products ADD BranchId int NULL;

    UPDATE p
    SET p.BranchId = x.BranchId
    FROM dbo.Products p
    OUTER APPLY (
        SELECT TOP 1 COALESCE(agentUser.BranchId, customerUser.BranchId) AS BranchId
        FROM dbo.OrderDetails od
        INNER JOIN dbo.Orders o ON o.Id = od.OrderId
        LEFT JOIN dbo.Users agentUser ON agentUser.Id = o.AgentId
        LEFT JOIN dbo.Customers c ON c.Id = o.CustomerId
        LEFT JOIN dbo.Users customerUser ON customerUser.Id = c.UserId
        WHERE od.ProductId = p.Id
          AND COALESCE(agentUser.BranchId, customerUser.BranchId) IS NOT NULL
        ORDER BY o.CreatedAt DESC
    ) x
    WHERE p.BranchId IS NULL
      AND x.BranchId IS NOT NULL;

    DECLARE @DefaultBranchId int = (SELECT TOP 1 Id FROM dbo.Branches WHERE IsActive = 1 ORDER BY Id);
    IF @DefaultBranchId IS NOT NULL
    BEGIN
        UPDATE dbo.Products
        SET BranchId = @DefaultBranchId
        WHERE BranchId IS NULL;
    END

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('dbo.Products'))
        DROP INDEX IX_Products_SKU ON dbo.Products;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId' AND object_id = OBJECT_ID('dbo.Products'))
        CREATE INDEX IX_Products_BranchId ON dbo.Products(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId_SKU' AND object_id = OBJECT_ID('dbo.Products'))
        CREATE UNIQUE INDEX IX_Products_BranchId_SKU ON dbo.Products(BranchId, SKU) WHERE BranchId IS NOT NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Products_Branches_BranchId'
          AND parent_object_id = OBJECT_ID('dbo.Products')
    )
        ALTER TABLE dbo.Products
        WITH CHECK ADD CONSTRAINT FK_Products_Branches_BranchId
        FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id) ON DELETE SET NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Products','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Branches_BranchId' AND parent_object_id = OBJECT_ID('dbo.Products'))
        ALTER TABLE dbo.Products DROP CONSTRAINT FK_Products_Branches_BranchId;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId_SKU' AND object_id = OBJECT_ID('dbo.Products'))
        DROP INDEX IX_Products_BranchId_SKU ON dbo.Products;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId' AND object_id = OBJECT_ID('dbo.Products'))
        DROP INDEX IX_Products_BranchId ON dbo.Products;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('dbo.Products'))
        CREATE UNIQUE INDEX IX_Products_SKU ON dbo.Products(SKU);
END
");
        }
    }
}
