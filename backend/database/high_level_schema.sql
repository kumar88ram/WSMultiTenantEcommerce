/*
    High-level schema definition for SQL Server multi-tenant ecommerce solution.
    This script captures the central admin database (CentralAdminDB) and the tenant
    database (TenantDB). Use as a reference to generate migrations.
*/

------------------------------------------------------------
-- CentralAdminDB
------------------------------------------------------------
IF DB_ID(N'CentralAdminDB') IS NULL
BEGIN
    EXEC ('CREATE DATABASE CentralAdminDB');
END;
GO

USE CentralAdminDB;
GO

IF OBJECT_ID(N'dbo.Plans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Plans
    (
        PlanId          INT IDENTITY(1,1) PRIMARY KEY,
        PlanName        NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        MonthlyPrice    DECIMAL(10,2) NOT NULL DEFAULT 0,
        AnnualPrice     DECIMAL(10,2) NULL,
        MaxUsers        INT NULL,
        MaxProducts     INT NULL,
        MaxStorageMB    INT NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT UQ_Plans_PlanName UNIQUE (PlanName)
    );
END;
GO

IF OBJECT_ID(N'dbo.Plugins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Plugins
    (
        PluginId        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        PluginName      NVARCHAR(150) NOT NULL,
        Description     NVARCHAR(500) NULL,
        Version         NVARCHAR(50) NOT NULL,
        BillingModel    NVARCHAR(50) NOT NULL DEFAULT N'Included',
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT UQ_Plugins_PluginName UNIQUE (PluginName)
    );
END;
GO

IF OBJECT_ID(N'dbo.PlanPlugins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanPlugins
    (
        PlanId      INT NOT NULL,
        PluginId    UNIQUEIDENTIFIER NOT NULL,
        CreatedAt   DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        PRIMARY KEY (PlanId, PluginId),
        FOREIGN KEY (PlanId) REFERENCES dbo.Plans(PlanId),
        FOREIGN KEY (PluginId) REFERENCES dbo.Plugins(PluginId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        TenantId            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        TenantName          NVARCHAR(200) NOT NULL,
        DisplayName         NVARCHAR(200) NULL,
        Domain              NVARCHAR(255) NULL,
        Subdomain           NVARCHAR(100) NULL,
        PlanId              INT NOT NULL,
        TimeZone            NVARCHAR(100) NULL,
        Status              NVARCHAR(50) NOT NULL DEFAULT N'Active',
        ProvisionedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        TrialEndsAt         DATETIME2(0) NULL,
        BillingContactEmail NVARCHAR(255) NULL,
        Metadata            NVARCHAR(MAX) NULL,
        FOREIGN KEY (PlanId) REFERENCES dbo.Plans(PlanId),
        CONSTRAINT UQ_Tenants_Domain UNIQUE (Domain)
    );
END;
GO

IF OBJECT_ID(N'dbo.TenantPlugins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantPlugins
    (
        TenantId        UNIQUEIDENTIFIER NOT NULL,
        PluginId        UNIQUEIDENTIFIER NOT NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Active',
        EnabledAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        Configuration   NVARCHAR(MAX) NULL,
        PRIMARY KEY (TenantId, PluginId),
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        FOREIGN KEY (PluginId) REFERENCES dbo.Plugins(PluginId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Email           NVARCHAR(255) NOT NULL,
        PasswordHash    VARBINARY(512) NOT NULL,
        DisplayName     NVARCHAR(200) NULL,
        Role            NVARCHAR(50) NOT NULL DEFAULT N'SuperAdmin',
        IsActive        BIT NOT NULL DEFAULT 1,
        LastLoginAt     DATETIME2(0) NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END;
GO

IF OBJECT_ID(N'dbo.TenantEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantEvents
    (
        TenantEventId  BIGINT IDENTITY(1,1) PRIMARY KEY,
        TenantId       UNIQUEIDENTIFIER NOT NULL,
        EventType      NVARCHAR(100) NOT NULL,
        EventPayload   NVARCHAR(MAX) NULL,
        CreatedBy      UNIQUEIDENTIFIER NULL,
        CreatedAt      DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId)
    );
END;
GO

------------------------------------------------------------
-- TenantDB
------------------------------------------------------------
IF DB_ID(N'TenantDB_Template') IS NULL
BEGIN
    EXEC ('CREATE DATABASE TenantDB_Template');
END;
GO

USE TenantDB_Template;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Email           NVARCHAR(255) NOT NULL,
        PasswordHash    VARBINARY(512) NULL,
        FirstName       NVARCHAR(100) NULL,
        LastName        NVARCHAR(100) NULL,
        PhoneNumber     NVARCHAR(50) NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Active',
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        LastLoginAt     DATETIME2(0) NULL,
        CONSTRAINT UQ_Customers_Email UNIQUE (Email)
    );
END;
GO

IF OBJECT_ID(N'dbo.Addresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Addresses
    (
        AddressId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        CustomerId      UNIQUEIDENTIFIER NULL,
        Label           NVARCHAR(100) NULL,
        FirstName       NVARCHAR(100) NOT NULL,
        LastName        NVARCHAR(100) NOT NULL,
        Company         NVARCHAR(150) NULL,
        AddressLine1    NVARCHAR(200) NOT NULL,
        AddressLine2    NVARCHAR(200) NULL,
        City            NVARCHAR(100) NOT NULL,
        StateProvince   NVARCHAR(100) NULL,
        PostalCode      NVARCHAR(20) NULL,
        CountryCode     NVARCHAR(2) NOT NULL,
        PhoneNumber     NVARCHAR(50) NULL,
        IsDefault       BIT NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryId      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ParentCategoryId UNIQUEIDENTIFIER NULL,
        Name            NVARCHAR(150) NOT NULL,
        Slug            NVARCHAR(150) NOT NULL,
        Description     NVARCHAR(500) NULL,
        SortOrder       INT NOT NULL DEFAULT 0,
        IsVisible       BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Categories(CategoryId),
        CONSTRAINT UQ_Categories_Slug UNIQUE (Slug)
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name            NVARCHAR(200) NOT NULL,
        Slug            NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        SKU             NVARCHAR(100) NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Draft',
        Visibility      NVARCHAR(50) NOT NULL DEFAULT N'Catalog',
        Brand           NVARCHAR(150) NULL,
        WeightGrams     INT NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        PublishedAt     DATETIME2(0) NULL,
        CONSTRAINT UQ_Products_Slug UNIQUE (Slug)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategories
    (
        ProductId   UNIQUEIDENTIFIER NOT NULL,
        CategoryId  UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (ProductId, CategoryId),
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Media', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Media
    (
        MediaId         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        FileName        NVARCHAR(255) NOT NULL,
        Url             NVARCHAR(500) NOT NULL,
        ContentType     NVARCHAR(100) NOT NULL,
        SizeBytes       BIGINT NOT NULL,
        AltText         NVARCHAR(255) NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductMedia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductMedia
    (
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        MediaId         UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL DEFAULT 0,
        IsPrimary       BIT NOT NULL DEFAULT 0,
        PRIMARY KEY (ProductId, MediaId),
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        FOREIGN KEY (MediaId) REFERENCES dbo.Media(MediaId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductAttributes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductAttributes
    (
        AttributeId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name            NVARCHAR(150) NOT NULL,
        Description     NVARCHAR(500) NULL,
        DataType        NVARCHAR(50) NOT NULL,
        IsVariantOption BIT NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductAttributeValues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductAttributeValues
    (
        AttributeValueId    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        AttributeId         UNIQUEIDENTIFIER NOT NULL,
        ProductId           UNIQUEIDENTIFIER NOT NULL,
        Value               NVARCHAR(4000) NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (AttributeId) REFERENCES dbo.ProductAttributes(AttributeId),
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Variants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Variants
    (
        VariantId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        SKU             NVARCHAR(100) NOT NULL,
        Barcode         NVARCHAR(100) NULL,
        Price           DECIMAL(12,2) NOT NULL,
        CompareAtPrice  DECIMAL(12,2) NULL,
        Cost            DECIMAL(12,2) NULL,
        WeightGrams     INT NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT UQ_Variants_SKU UNIQUE (SKU)
    );
END;
GO

IF OBJECT_ID(N'dbo.VariantOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VariantOptions
    (
        VariantOptionId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        VariantId       UNIQUEIDENTIFIER NOT NULL,
        AttributeId     UNIQUEIDENTIFIER NOT NULL,
        Value           NVARCHAR(200) NOT NULL,
        FOREIGN KEY (VariantId) REFERENCES dbo.Variants(VariantId),
        FOREIGN KEY (AttributeId) REFERENCES dbo.ProductAttributes(AttributeId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Inventory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventory
    (
        InventoryId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        VariantId       UNIQUEIDENTIFIER NOT NULL,
        LocationCode    NVARCHAR(100) NOT NULL,
        QuantityOnHand  INT NOT NULL,
        QuantityReserved INT NOT NULL DEFAULT 0,
        ReorderPoint    INT NULL,
        UpdatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (VariantId) REFERENCES dbo.Variants(VariantId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Coupons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Coupons
    (
        CouponId        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Code            NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        DiscountType    NVARCHAR(50) NOT NULL,
        DiscountValue   DECIMAL(12,2) NOT NULL,
        MaxRedemptions  INT NULL,
        PerCustomerLimit INT NULL,
        StartsAt        DATETIME2(0) NULL,
        ExpiresAt       DATETIME2(0) NULL,
        MinimumOrderValue DECIMAL(12,2) NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Active',
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        CONSTRAINT UQ_Coupons_Code UNIQUE (Code)
    );
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        OrderNumber     NVARCHAR(50) NOT NULL,
        CustomerId      UNIQUEIDENTIFIER NULL,
        Status          NVARCHAR(50) NOT NULL,
        OrderDate       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        CurrencyCode    NVARCHAR(3) NOT NULL DEFAULT N'USD',
        Subtotal        DECIMAL(12,2) NOT NULL DEFAULT 0,
        DiscountTotal   DECIMAL(12,2) NOT NULL DEFAULT 0,
        TaxTotal        DECIMAL(12,2) NOT NULL DEFAULT 0,
        ShippingTotal   DECIMAL(12,2) NOT NULL DEFAULT 0,
        Total           DECIMAL(12,2) NOT NULL DEFAULT 0,
        ShippingAddressId UNIQUEIDENTIFIER NULL,
        BillingAddressId  UNIQUEIDENTIFIER NULL,
        CouponId        UNIQUEIDENTIFIER NULL,
        Notes           NVARCHAR(MAX) NULL,
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        FOREIGN KEY (ShippingAddressId) REFERENCES dbo.Addresses(AddressId),
        FOREIGN KEY (BillingAddressId) REFERENCES dbo.Addresses(AddressId),
        FOREIGN KEY (CouponId) REFERENCES dbo.Coupons(CouponId),
        CONSTRAINT UQ_Orders_OrderNumber UNIQUE (OrderNumber)
    );
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        OrderId         UNIQUEIDENTIFIER NOT NULL,
        VariantId       UNIQUEIDENTIFIER NOT NULL,
        Quantity        INT NOT NULL,
        UnitPrice       DECIMAL(12,2) NOT NULL,
        DiscountAmount  DECIMAL(12,2) NOT NULL DEFAULT 0,
        TaxAmount       DECIMAL(12,2) NOT NULL DEFAULT 0,
        Total           DECIMAL(12,2) NOT NULL,
        FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
        FOREIGN KEY (VariantId) REFERENCES dbo.Variants(VariantId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ShippingZones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShippingZones
    (
        ShippingZoneId  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name            NVARCHAR(150) NOT NULL,
        Countries       NVARCHAR(MAX) NOT NULL, -- comma separated ISO codes or JSON
        PricingStrategy NVARCHAR(50) NOT NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.ShippingRates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShippingRates
    (
        ShippingRateId  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ShippingZoneId  UNIQUEIDENTIFIER NOT NULL,
        MinWeightGrams  INT NULL,
        MaxWeightGrams  INT NULL,
        MinOrderTotal   DECIMAL(12,2) NULL,
        MaxOrderTotal   DECIMAL(12,2) NULL,
        Rate            DECIMAL(12,2) NOT NULL,
        RateName        NVARCHAR(150) NOT NULL,
        FOREIGN KEY (ShippingZoneId) REFERENCES dbo.ShippingZones(ShippingZoneId)
    );
END;
GO

IF OBJECT_ID(N'dbo.SupportTickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTickets
    (
        TicketId        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        CustomerId      UNIQUEIDENTIFIER NULL,
        Subject         NVARCHAR(200) NOT NULL,
        Message         NVARCHAR(MAX) NOT NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Open',
        Priority        NVARCHAR(50) NULL,
        AssignedTo      NVARCHAR(200) NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews
    (
        ReviewId        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        CustomerId      UNIQUEIDENTIFIER NULL,
        Rating          TINYINT NOT NULL,
        Title           NVARCHAR(200) NULL,
        Content         NVARCHAR(MAX) NULL,
        Status          NVARCHAR(50) NOT NULL DEFAULT N'Pending',
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
END;
GO

IF OBJECT_ID(N'dbo.OrderEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderEvents
    (
        OrderEventId    BIGINT IDENTITY(1,1) PRIMARY KEY,
        OrderId         UNIQUEIDENTIFIER NOT NULL,
        EventType       NVARCHAR(100) NOT NULL,
        EventPayload    NVARCHAR(MAX) NULL,
        CreatedAt       DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId)
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryAdjustments
    (
        AdjustmentId    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        InventoryId     UNIQUEIDENTIFIER NOT NULL,
        QuantityChange  INT NOT NULL,
        Reason          NVARCHAR(200) NULL,
        AdjustedBy      NVARCHAR(200) NULL,
        AdjustedAt      DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (InventoryId) REFERENCES dbo.Inventory(InventoryId)
    );
END;
GO

/*
    Additional indexes and constraints should be introduced by migrations
    based on workload insights. Tenant-specific databases can be created
    from the TenantDB_Template structure.
*/
