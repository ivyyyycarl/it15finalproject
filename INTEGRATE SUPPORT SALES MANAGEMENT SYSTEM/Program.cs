using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Configuration;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Data;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.DTOs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Services;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Middleware;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Hubs;
using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiErrorResponseFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var response = ApiErrorFactory.Create(
            StatusCodes.Status400BadRequest,
            "Validation failed for one or more fields.",
            context.HttpContext.TraceIdentifier,
            errors);

        return new BadRequestObjectResult(response);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration["DatabaseSettings:ConnectionString"],
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

builder.Services.AddSignalR();
builder.Services.AddScoped<IDataChangeNotifier, DataChangeNotifier>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                      ?? new[] {
                          "https://classicfit-app.runasp.net", "http://classicfit-app.runasp.net",
                          "https://site54750.siteasp.net", "http://site54750.siteasp.net",
                          "http://localhost:5173", "https://localhost:5173",
                          "http://localhost:5000", "https://localhost:5001",
                          "http://localhost:5250", "https://localhost:5250",
                          "http://localhost:5300", "https://localhost:7300"
                      };
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var configuredJwtKey = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    configuredJwtKey = builder.Configuration["JwtSettings__Key"];
}
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    configuredJwtKey = Environment.GetEnvironmentVariable("JWT__KEY")
        ?? Environment.GetEnvironmentVariable("JwtSettings__Key");
}
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        // Development fallback so local logins work even when secrets are not configured.
        configuredJwtKey = "classicfit-dev-jwt-key-change-before-production-2026";
    }
    else
    {
        throw new InvalidOperationException(
            "JWT signing key is missing. Configure JwtSettings:Key (or JWT__KEY) before starting the application.");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredJwtKey))
        };
    });

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IErpInventoryService, ErpInventoryService>();
builder.Services.AddScoped<IErpFinanceService, ErpFinanceService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<IModuleManagementService, ModuleManagementService>();
builder.Services.AddHostedService<AutomationBackgroundWorker>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// Configure Stripe from secure configuration/environment.
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrWhiteSpace(stripeSecretKey))
{
    stripeSecretKey = builder.Configuration["Stripe__SecretKey"];
}
if (string.IsNullOrWhiteSpace(stripeSecretKey))
{
    stripeSecretKey = Environment.GetEnvironmentVariable("Stripe__SecretKey");
}
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Prevent stale shell/index caching so latest frontend bundles are always loaded.
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/") || context.Request.Path.Equals("/index.html"))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    await next();
});

app.UseRateLimiter();
app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    var statusCode = httpContext.Response.StatusCode;

    if (statusCode < 400 || httpContext.Response.HasStarted)
    {
        return;
    }

    httpContext.Response.ContentType = "application/json";
    var payload = ApiErrorFactory.Create(
        statusCode,
        ApiErrorFactory.GetDefaultMessage(statusCode),
        httpContext.TraceIdentifier);

    await httpContext.Response.WriteAsJsonAsync(payload);
});

var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
contentTypeProvider.Mappings[".blat"] = "application/octet-stream";
contentTypeProvider.Mappings[".dll"] = "application/octet-stream";
contentTypeProvider.Mappings[".wasm"] = "application/wasm";
contentTypeProvider.Mappings[".json"] = "application/json";
contentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<SupportCallHub>("/hubs/support-call");

app.MapFallbackToFile("index.html");

// Seed default data and optional bootstrap SuperAdmin.
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    var seedEnabled = builder.Configuration.GetValue<bool>("BootstrapSuperAdmin:Enabled");
    var superAdminEmail = builder.Configuration["BootstrapSuperAdmin:Email"]?.Trim();
    var superAdminPassword = builder.Configuration["BootstrapSuperAdmin:Password"];
    var superAdminFirstName = builder.Configuration["BootstrapSuperAdmin:FirstName"]?.Trim();
    var superAdminLastName = builder.Configuration["BootstrapSuperAdmin:LastName"]?.Trim();
    var superAdminPhone = builder.Configuration["BootstrapSuperAdmin:Phone"]?.Trim();

    if (seedEnabled && !string.IsNullOrWhiteSpace(superAdminEmail) && !string.IsNullOrWhiteSpace(superAdminPassword))
    {
        var existingSuperAdmin = context.Users.FirstOrDefault(u => u.Email == superAdminEmail);
        if (existingSuperAdmin == null)
        {
            var superAdmin = new INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models.User
            {
                FirstName = string.IsNullOrWhiteSpace(superAdminFirstName) ? "Super" : superAdminFirstName,
                LastName = string.IsNullOrWhiteSpace(superAdminLastName) ? "Admin" : superAdminLastName,
                Email = superAdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(superAdminPassword),
                Phone = string.IsNullOrWhiteSpace(superAdminPhone) ? "9999999999" : superAdminPhone,
                Role = INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models.UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(superAdmin);
            context.SaveChanges();
        }
    }
    else if (seedEnabled)
    {
        app.Logger.LogWarning("BootstrapSuperAdmin is enabled but email/password is not configured. Skipping bootstrap account seed.");
    }

    void UpsertPlan(
        string name,
        string code,
        string description,
        decimal monthlyPrice,
        decimal annualPrice,
        int maxUsers,
        int maxBranches,
        string includedModulesCsv)
    {
        var existingPlan = context.SubscriptionPlans.FirstOrDefault(p => p.Code == code);
        if (existingPlan == null)
        {
            context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Name = name,
                Code = code,
                Description = description,
                MonthlyPrice = monthlyPrice,
                AnnualPrice = annualPrice,
                MaxUsers = maxUsers,
                MaxBranches = maxBranches,
                IncludedModulesCsv = includedModulesCsv,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        existingPlan.Name = name;
        existingPlan.Description = description;
        existingPlan.MonthlyPrice = monthlyPrice;
        existingPlan.AnnualPrice = annualPrice;
        existingPlan.MaxUsers = maxUsers;
        existingPlan.MaxBranches = maxBranches;
        existingPlan.IncludedModulesCsv = includedModulesCsv;
        existingPlan.IsActive = true;
        existingPlan.UpdatedAt = DateTime.UtcNow;
    }

    UpsertPlan(
        name: "Basic Plan",
        code: "BASIC",
        description: "Single-branch operations with standard ERP sync.",
        monthlyPrice: 999m,
        annualPrice: 9990m,
        maxUsers: 10,
        maxBranches: 1,
        includedModulesCsv: "dashboard,iam,catalog,orders,tickets,calls,customers,notifications");

    UpsertPlan(
        name: "Standard Plan",
        code: "STANDARD",
        description: "Multi-branch operations with advanced ERP sync.",
        monthlyPrice: 2499m,
        annualPrice: 24990m,
        maxUsers: 50,
        maxBranches: 3,
        includedModulesCsv: "dashboard,iam,catalog,orders,tickets,calls,customers,reports,notifications");

    UpsertPlan(
        name: "Enterprise Plan",
        code: "ENTERPRISE",
        description: "Unlimited users/branches with full ERP integration and analytics.",
        monthlyPrice: 4999m,
        annualPrice: 49990m,
        maxUsers: 0,
        maxBranches: 0,
        includedModulesCsv: "all");

    var canonicalPlanCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BASIC",
        "STANDARD",
        "ENTERPRISE"
    };
    var nonCanonicalActivePlans = context.SubscriptionPlans
        .Where(p => p.IsActive && !canonicalPlanCodes.Contains(p.Code))
        .ToList();
    foreach (var plan in nonCanonicalActivePlans)
    {
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;
    }

    context.SaveChanges();

    // Safety patch for environments where an older migration created ModuleDefinitions
    // without role flag columns. This keeps module governance endpoints operational.
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.ModuleDefinitions','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ModuleDefinitions','AllowAdmin') IS NULL
        ALTER TABLE dbo.ModuleDefinitions ADD AllowAdmin bit NOT NULL CONSTRAINT DF_ModuleDefinitions_AllowAdmin DEFAULT(1);
    IF COL_LENGTH('dbo.ModuleDefinitions','AllowSupervisor') IS NULL
        ALTER TABLE dbo.ModuleDefinitions ADD AllowSupervisor bit NOT NULL CONSTRAINT DF_ModuleDefinitions_AllowSupervisor DEFAULT(1);
    IF COL_LENGTH('dbo.ModuleDefinitions','AllowAgent') IS NULL
        ALTER TABLE dbo.ModuleDefinitions ADD AllowAgent bit NOT NULL CONSTRAINT DF_ModuleDefinitions_AllowAgent DEFAULT(1);
    IF COL_LENGTH('dbo.ModuleDefinitions','AllowCustomer') IS NULL
        ALTER TABLE dbo.ModuleDefinitions ADD AllowCustomer bit NOT NULL CONSTRAINT DF_ModuleDefinitions_AllowCustomer DEFAULT(1);
END
");

    // Safety patch for phone columns to support international formats.
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Users','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users','Phone') IS NULL
        EXEC(N'ALTER TABLE dbo.Users ADD Phone nvarchar(20) NOT NULL CONSTRAINT DF_Users_Phone DEFAULT('''');');
    ELSE IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Users')
          AND name = 'Phone'
          AND max_length > 0
          AND max_length < 40
    )
        EXEC(N'ALTER TABLE dbo.Users ALTER COLUMN Phone nvarchar(20) NOT NULL;');
END

IF OBJECT_ID('dbo.Customers','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Customers','Phone') IS NOT NULL
       AND EXISTS (
            SELECT 1
            FROM sys.columns
            WHERE object_id = OBJECT_ID('dbo.Customers')
              AND name = 'Phone'
              AND max_length > 0
              AND max_length < 40
       )
        EXEC(N'ALTER TABLE dbo.Customers ALTER COLUMN Phone nvarchar(20) NULL;');
END
");

    // Safety patch for product branch ownership so product/inventory data can be branch-scoped.
    // Keep this patch minimal and dynamic to avoid SQL Server compile-time column resolution issues.
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Products','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Products','BranchId') IS NULL
        EXEC(N'ALTER TABLE dbo.Products ADD BranchId int NULL;');

    IF COL_LENGTH('dbo.Products','BranchId') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('dbo.Products'))
            EXEC(N'DROP INDEX IX_Products_SKU ON dbo.Products;');

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId' AND object_id = OBJECT_ID('dbo.Products'))
            EXEC(N'CREATE INDEX IX_Products_BranchId ON dbo.Products(BranchId);');

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_BranchId_SKU' AND object_id = OBJECT_ID('dbo.Products'))
            EXEC(N'CREATE UNIQUE INDEX IX_Products_BranchId_SKU ON dbo.Products(BranchId, SKU) WHERE BranchId IS NOT NULL;');

        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = 'FK_Products_Branches_BranchId'
              AND parent_object_id = OBJECT_ID('dbo.Products')
        )
            EXEC(N'' +
                'ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT FK_Products_Branches_BranchId ' +
                'FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id) ON DELETE SET NULL;');
    END

    -- Ensure Description and ImageUrl columns can hold enough characters
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Description' AND max_length < 4000 AND max_length != -1
    )
        EXEC(N'ALTER TABLE dbo.Products ALTER COLUMN Description nvarchar(2000) NULL;');

    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'ImageUrl' AND max_length < 4000 AND max_length != -1
    )
        EXEC(N'ALTER TABLE dbo.Products ALTER COLUMN ImageUrl nvarchar(2000) NULL;');
END
");

    // Safety patch for refund request workflow table and schema drift.
    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.RefundRequests','U') IS NULL
BEGIN
    CREATE TABLE dbo.RefundRequests
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId int NOT NULL,
        RequestedByUserId int NOT NULL,
        Reason nvarchar(1000) NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_RefundRequests_Status DEFAULT('Pending'),
        ApprovedByUserId int NULL,
        ApprovedAt datetime2 NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_RefundRequests_CreatedAt DEFAULT(GETUTCDATE()),
        UpdatedAt datetime2 NULL
    );
END

IF OBJECT_ID('dbo.RefundRequests','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.RefundRequests','OrderId') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD OrderId int NOT NULL CONSTRAINT DF_RefundRequests_OrderId DEFAULT(0);');
    IF COL_LENGTH('dbo.RefundRequests','RequestedByUserId') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD RequestedByUserId int NOT NULL CONSTRAINT DF_RefundRequests_RequestedByUserId DEFAULT(0);');
    IF COL_LENGTH('dbo.RefundRequests','Reason') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD Reason nvarchar(1000) NULL;');
    IF COL_LENGTH('dbo.RefundRequests','Status') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD Status nvarchar(30) NOT NULL CONSTRAINT DF_RefundRequests_Status DEFAULT(''Pending'');');
    IF COL_LENGTH('dbo.RefundRequests','ApprovedByUserId') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD ApprovedByUserId int NULL;');
    IF COL_LENGTH('dbo.RefundRequests','ApprovedAt') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD ApprovedAt datetime2 NULL;');
    IF COL_LENGTH('dbo.RefundRequests','CreatedAt') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CreatedAt datetime2 NOT NULL CONSTRAINT DF_RefundRequests_CreatedAt DEFAULT(GETUTCDATE());');
    IF COL_LENGTH('dbo.RefundRequests','UpdatedAt') IS NULL
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD UpdatedAt datetime2 NULL;');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.RefundRequests')
          AND c.name = 'Status'
    )
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CONSTRAINT DF_RefundRequests_Status DEFAULT(''Pending'') FOR [Status];');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.RefundRequests')
          AND c.name = 'CreatedAt'
    )
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CONSTRAINT DF_RefundRequests_CreatedAt DEFAULT(GETUTCDATE()) FOR [CreatedAt];');

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_RefundRequests_Orders_OrderId'
          AND parent_object_id = OBJECT_ID('dbo.RefundRequests')
    )
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CONSTRAINT FK_RefundRequests_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE;');

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_RefundRequests_Users_RequestedByUserId'
          AND parent_object_id = OBJECT_ID('dbo.RefundRequests')
    )
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CONSTRAINT FK_RefundRequests_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id);');

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_RefundRequests_Users_ApprovedByUserId'
          AND parent_object_id = OBJECT_ID('dbo.RefundRequests')
    )
        EXEC(N'ALTER TABLE dbo.RefundRequests ADD CONSTRAINT FK_RefundRequests_Users_ApprovedByUserId FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(Id);');

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_RefundRequests_OrderId_Status'
          AND object_id = OBJECT_ID('dbo.RefundRequests')
    )
        EXEC(N'CREATE INDEX IX_RefundRequests_OrderId_Status ON dbo.RefundRequests(OrderId, Status);');

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_RefundRequests_CreatedAt'
          AND object_id = OBJECT_ID('dbo.RefundRequests')
    )
        EXEC(N'CREATE INDEX IX_RefundRequests_CreatedAt ON dbo.RefundRequests(CreatedAt);');
END
");

    // Bootstrap normalized module catalog and plan entitlements.
    var moduleManagementService = scope.ServiceProvider.GetRequiredService<IModuleManagementService>();
    var moduleConfig = moduleManagementService.GetConfigurationAsync().GetAwaiter().GetResult();
    var moduleCatalog = moduleConfig?.Modules ?? new List<ModuleAccessItemDto>();

    if (moduleCatalog.Count > 0)
    {
        var existingModuleDefs = context.ModuleDefinitions.ToList();
        foreach (var module in moduleCatalog)
        {
            var moduleKey = module.ModuleKey?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(moduleKey))
            {
                continue;
            }

            var found = existingModuleDefs.FirstOrDefault(m => m.ModuleKey == moduleKey);
            if (found == null)
            {
                found = new ModuleDefinition
                {
                    ModuleKey = moduleKey,
                    DisplayName = module.DisplayName?.Trim() ?? moduleKey,
                    Description = module.Description?.Trim(),
                    Category = string.IsNullOrWhiteSpace(module.Category) ? "General" : module.Category.Trim(),
                    IsActive = module.IsEnabled,
                    AllowAdmin = module.RoleAccess?.Admin ?? true,
                    AllowSupervisor = module.RoleAccess?.Supervisor ?? true,
                    AllowAgent = module.RoleAccess?.Agent ?? true,
                    AllowCustomer = module.RoleAccess?.Customer ?? true,
                    CreatedAt = DateTime.UtcNow
                };
                context.ModuleDefinitions.Add(found);
                existingModuleDefs.Add(found);
            }
            else
            {
                found.DisplayName = module.DisplayName?.Trim() ?? found.DisplayName;
                found.Description = module.Description?.Trim();
                found.Category = string.IsNullOrWhiteSpace(module.Category) ? found.Category : module.Category.Trim();
                found.IsActive = module.IsEnabled;
                found.AllowAdmin = module.RoleAccess?.Admin ?? found.AllowAdmin;
                found.AllowSupervisor = module.RoleAccess?.Supervisor ?? found.AllowSupervisor;
                found.AllowAgent = module.RoleAccess?.Agent ?? found.AllowAgent;
                found.AllowCustomer = module.RoleAccess?.Customer ?? found.AllowCustomer;
                found.UpdatedAt = DateTime.UtcNow;
            }

            if (string.Equals(found.ModuleKey, "iam", StringComparison.OrdinalIgnoreCase))
            {
                found.AllowAdmin = true;
            }
        }
        context.SaveChanges();

        var plans = context.SubscriptionPlans.AsNoTracking().ToList();
        var moduleDefinitions = context.ModuleDefinitions.AsNoTracking().ToList();
        var existingEntitlements = context.PlanModuleEntitlements.ToList();

        foreach (var plan in plans)
        {
            var includedTokens = (plan.IncludedModulesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToList();

            var allIncluded = includedTokens.Count == 0 || includedTokens.Any(token => token.Equals("All", StringComparison.OrdinalIgnoreCase));
            var normalizedIncluded = includedTokens
                .Select(token => token.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var moduleDef in moduleDefinitions)
            {
                var moduleConfigItem = moduleCatalog.FirstOrDefault(m =>
                    string.Equals(m.ModuleKey, moduleDef.ModuleKey, StringComparison.OrdinalIgnoreCase));

                var isIncluded = allIncluded || normalizedIncluded.Contains(moduleDef.ModuleKey);
                if (string.Equals(moduleDef.ModuleKey, "iam", StringComparison.OrdinalIgnoreCase))
                {
                    // User management must be available in every subscription plan.
                    isIncluded = true;
                }
                var foundEntitlement = existingEntitlements.FirstOrDefault(e =>
                    e.SubscriptionPlanId == plan.Id && e.ModuleDefinitionId == moduleDef.Id);

                var allowAdmin = moduleConfigItem?.RoleAccess?.Admin ?? true;
                if (string.Equals(moduleDef.ModuleKey, "iam", StringComparison.OrdinalIgnoreCase))
                {
                    // Admins must be able to create supervisors and agents for all plans.
                    allowAdmin = true;
                }

                if (foundEntitlement == null)
                {
                    foundEntitlement = new PlanModuleEntitlement
                    {
                        SubscriptionPlanId = plan.Id,
                        ModuleDefinitionId = moduleDef.Id,
                        IsIncluded = isIncluded,
                        AllowAdmin = allowAdmin,
                        AllowSupervisor = moduleConfigItem?.RoleAccess?.Supervisor ?? true,
                        AllowAgent = moduleConfigItem?.RoleAccess?.Agent ?? true,
                        AllowCustomer = moduleConfigItem?.RoleAccess?.Customer ?? true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.PlanModuleEntitlements.Add(foundEntitlement);
                    existingEntitlements.Add(foundEntitlement);
                }
                else
                {
                    foundEntitlement.IsIncluded = isIncluded;
                    foundEntitlement.AllowAdmin = allowAdmin;
                    foundEntitlement.AllowSupervisor = moduleConfigItem?.RoleAccess?.Supervisor ?? foundEntitlement.AllowSupervisor;
                    foundEntitlement.AllowAgent = moduleConfigItem?.RoleAccess?.Agent ?? foundEntitlement.AllowAgent;
                    foundEntitlement.AllowCustomer = moduleConfigItem?.RoleAccess?.Customer ?? foundEntitlement.AllowCustomer;
                    foundEntitlement.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        context.SaveChanges();
    }

    if (!context.TenantSubscriptions.Any())
    {
        var defaultPlan = context.SubscriptionPlans.FirstOrDefault(p => p.Code == "STANDARD")
                          ?? context.SubscriptionPlans.OrderBy(p => p.Id).First();

        context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantName = "ClassicFit",
            SubscriptionPlanId = defaultPlan.Id,
            Status = SubscriptionStatus.Active,
            StartsAt = DateTime.UtcNow,
            NextBillingAt = DateTime.UtcNow.AddMonths(1),
            AutoRenew = true
        });
        context.SaveChanges();
    }

    // Idempotent data backfill:
    // - Ensure existing customers have a company value.
    // - Ensure linked customer users have a branch assignment.
    var tenantNameForBackfill = context.TenantSubscriptions
        .AsNoTracking()
        .Where(t => !string.IsNullOrWhiteSpace(t.TenantName))
        .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
        .Select(t => t.TenantName.Trim())
        .FirstOrDefault();

    var fallbackBranchId = context.Branches
        .AsNoTracking()
        .Where(b => b.IsActive)
        .OrderBy(b => b.Id)
        .Select(b => (int?)b.Id)
        .FirstOrDefault();

    var customersForBackfill = context.Customers
        .Include(c => c.User)
        .Include(c => c.CreatedByUser)
        .ToList();

    var backfillChanged = false;
    var nowUtc = DateTime.UtcNow;

    foreach (var customer in customersForBackfill)
    {
        var customerChanged = false;

        if (string.IsNullOrWhiteSpace(customer.Company) && !string.IsNullOrWhiteSpace(tenantNameForBackfill))
        {
            customer.Company = tenantNameForBackfill;
            customerChanged = true;
        }

        if (customer.User != null && !customer.User.BranchId.HasValue)
        {
            var resolvedBranchId = customer.CreatedByUser?.BranchId;
            if (!resolvedBranchId.HasValue)
            {
                resolvedBranchId = fallbackBranchId;
            }

            if (resolvedBranchId.HasValue)
            {
                customer.User.BranchId = resolvedBranchId.Value;
                customer.User.UpdatedAt = nowUtc;
                customerChanged = true;
            }
        }

        if (customerChanged)
        {
            customer.UpdatedAt = nowUtc;
            backfillChanged = true;
        }
    }

    if (backfillChanged)
    {
        context.SaveChanges();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: Database seed failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    app.Logger.LogWarning(ex, "Database seed failed - will retry on next startup");
}
app.Run();