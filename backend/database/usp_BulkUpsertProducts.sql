/*
    This script defines a table-valued type and stored procedure that can be used
    to bulk import or upsert products, categories, and inventory records for the
    multi-tenant catalog. The procedure expects attribute and category payloads
    to be provided as delimited text (matching the CSV parser implemented in
    ProductCatalogService).
*/
GO
IF TYPE_ID(N'dbo.ProductImportType') IS NOT NULL
BEGIN
    DROP TYPE dbo.ProductImportType;
END
GO
CREATE TYPE dbo.ProductImportType AS TABLE
(
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CompareAtPrice DECIMAL(18,2) NULL,
    IsPublished BIT NOT NULL,
    CategorySlugs NVARCHAR(2000) NULL,
    AttributePayload NVARCHAR(MAX) NULL,
    InventoryQuantity INT NOT NULL
);
GO
CREATE OR ALTER PROCEDURE dbo.usp_BulkUpsertProducts
    @Products dbo.ProductImportType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Upserted TABLE (ProductId UNIQUEIDENTIFIER, TenantId UNIQUEIDENTIFIER, Slug NVARCHAR(200));

    MERGE INTO Products AS Target
    USING @Products AS Source
        ON Target.TenantId = Source.TenantId AND Target.Slug = Source.Slug
    WHEN MATCHED THEN
        UPDATE SET
            Target.Name = Source.Name,
            Target.Description = Source.Description,
            Target.Price = Source.Price,
            Target.CompareAtPrice = Source.CompareAtPrice,
            Target.IsPublished = Source.IsPublished,
            Target.UpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Id, TenantId, Name, Slug, Description, Price, CompareAtPrice, IsPublished, CreatedAt)
        VALUES (NEWID(), Source.TenantId, Source.Name, Source.Slug, Source.Description, Source.Price, Source.CompareAtPrice, Source.IsPublished, SYSUTCDATETIME())
    OUTPUT inserted.Id, inserted.TenantId, inserted.Slug INTO @Upserted;

    -- Ensure product-category mappings
    ;WITH ProductCategories AS (
        SELECT U.ProductId,
               STRING_SPLIT(P.CategorySlugs, ';') AS CategorySlug
        FROM @Upserted U
        JOIN @Products P ON P.Slug = U.Slug AND P.TenantId = U.TenantId
    )
    INSERT INTO ProductCategories (ProductId, CategoryId, TenantId)
    SELECT DISTINCT pc.ProductId, c.Id, c.TenantId
    FROM ProductCategories pc
    JOIN Categories c ON c.Slug = pc.CategorySlug AND c.TenantId = (SELECT TenantId FROM @Upserted WHERE ProductId = pc.ProductId)
    WHERE NOT EXISTS (
        SELECT 1
        FROM ProductCategories existing
        WHERE existing.ProductId = pc.ProductId
          AND existing.CategoryId = c.Id
    );

    -- Clean up removed product-category relationships
    DELETE pc
    FROM ProductCategories pc
    JOIN @Upserted u ON u.ProductId = pc.ProductId
    LEFT JOIN STRING_SPLIT((SELECT CategorySlugs FROM @Products WHERE Slug = u.Slug AND TenantId = u.TenantId), ';') cs
        ON cs.value = (SELECT Slug FROM Categories WHERE Id = pc.CategoryId)
    WHERE cs.value IS NULL;

    -- Handle inventory upsert
    MERGE INTO Inventories AS Target
    USING (
        SELECT u.ProductId,
               p.InventoryQuantity,
               u.TenantId
        FROM @Upserted u
        JOIN @Products p ON p.Slug = u.Slug AND p.TenantId = u.TenantId
    ) AS Source(ProductId, QuantityOnHand, TenantId)
    ON Target.ProductId = Source.ProductId AND Target.ProductVariantId IS NULL AND Target.TenantId = Source.TenantId
    WHEN MATCHED THEN
        UPDATE SET Target.QuantityOnHand = Source.QuantityOnHand,
                   Target.ReservedQuantity = 0,
                   Target.LastAdjustedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (Id, TenantId, ProductId, QuantityOnHand, ReservedQuantity, LastAdjustedAt)
        VALUES (NEWID(), Source.TenantId, Source.ProductId, Source.QuantityOnHand, 0, SYSUTCDATETIME());

    -- Attribute handling - parse payloads and upsert definitions/values
    DECLARE AttributeCursor CURSOR FAST_FORWARD FOR
        SELECT u.ProductId,
               p.TenantId,
               p.AttributePayload
        FROM @Upserted u
        JOIN @Products p ON p.Slug = u.Slug AND p.TenantId = u.TenantId
        WHERE p.AttributePayload IS NOT NULL AND LEN(p.AttributePayload) > 0;

    DECLARE @ProductId UNIQUEIDENTIFIER;
    DECLARE @TenantId UNIQUEIDENTIFIER;
    DECLARE @Payload NVARCHAR(MAX);

    OPEN AttributeCursor;
    FETCH NEXT FROM AttributeCursor INTO @ProductId, @TenantId, @Payload;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @Segments TABLE (AttributeName NVARCHAR(150), Value NVARCHAR(400));

        DECLARE @pos INT = 1;
        DECLARE @segment NVARCHAR(MAX);
        WHILE @pos <= LEN(@Payload)
        BEGIN
            DECLARE @next INT = CHARINDEX(';', @Payload, @pos);
            IF @next = 0 SET @next = LEN(@Payload) + 1;
            SET @segment = LTRIM(RTRIM(SUBSTRING(@Payload, @pos, @next - @pos)));
            IF LEN(@segment) > 0
            BEGIN
                DECLARE @eq INT = CHARINDEX('=', @segment);
                IF @eq > 0
                BEGIN
                    DECLARE @attr NVARCHAR(150) = SUBSTRING(@segment, 1, @eq - 1);
                    DECLARE @vals NVARCHAR(1000) = SUBSTRING(@segment, @eq + 1, LEN(@segment));
                    DECLARE @valPos INT = 1;
                    WHILE @valPos <= LEN(@vals)
                    BEGIN
                        DECLARE @valNext INT = CHARINDEX('|', @vals, @valPos);
                        IF @valNext = 0 SET @valNext = LEN(@vals) + 1;
                        DECLARE @value NVARCHAR(400) = LTRIM(RTRIM(SUBSTRING(@vals, @valPos, @valNext - @valPos)));
                        IF LEN(@value) > 0
                        BEGIN
                            INSERT INTO @Segments(AttributeName, Value) VALUES (@attr, @value);
                        END
                        SET @valPos = @valNext + 1;
                    END
                END
            END
            SET @pos = @next + 1;
        END

        DECLARE AttributeCursor2 CURSOR FAST_FORWARD FOR
            SELECT AttributeName, Value FROM @Segments;
        DECLARE @AttrName NVARCHAR(150);
        DECLARE @AttrValue NVARCHAR(400);

        OPEN AttributeCursor2;
        FETCH NEXT FROM AttributeCursor2 INTO @AttrName, @AttrValue;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @AttributeId UNIQUEIDENTIFIER;
            SELECT @AttributeId = Id
            FROM ProductAttributes
            WHERE TenantId = @TenantId AND ProductId = @ProductId AND Name = @AttrName;

            IF @AttributeId IS NULL
            BEGIN
                SET @AttributeId = NEWID();
                INSERT INTO ProductAttributes (Id, TenantId, ProductId, Name, DisplayName, CreatedAt)
                VALUES (@AttributeId, @TenantId, @ProductId, @AttrName, @AttrName, SYSUTCDATETIME());
            END

            IF NOT EXISTS (
                SELECT 1
                FROM AttributeValues
                WHERE TenantId = @TenantId AND ProductAttributeId = @AttributeId AND Value = @AttrValue)
            BEGIN
                INSERT INTO AttributeValues (Id, TenantId, ProductAttributeId, Value, SortOrder, CreatedAt)
                VALUES (NEWID(), @TenantId, @AttributeId, @AttrValue, 0, SYSUTCDATETIME());
            END

            FETCH NEXT FROM AttributeCursor2 INTO @AttrName, @AttrValue;
        END

        CLOSE AttributeCursor2;
        DEALLOCATE AttributeCursor2;

        FETCH NEXT FROM AttributeCursor INTO @ProductId, @TenantId, @Payload;
    END

    CLOSE AttributeCursor;
    DEALLOCATE AttributeCursor;
END
GO
