-- Sample shipping zones, methods, and rate tables
-- Replace the tenant identifier with the target tenant id before executing.
DECLARE @TenantId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

-- North America zone (default)
INSERT INTO ShippingZones (Id, TenantId, Name, IsDefault, CreatedAt)
VALUES ('22222222-2222-2222-2222-222222222222', @TenantId, 'North America', 1, SYSUTCDATETIME());

INSERT INTO ShippingZoneRegions (Id, TenantId, ShippingZoneId, CountryCode, StateCode, CreatedAt)
VALUES
    ('33333333-3333-3333-3333-333333333333', @TenantId, '22222222-2222-2222-2222-222222222222', 'US', NULL, SYSUTCDATETIME()),
    ('44444444-4444-4444-4444-444444444444', @TenantId, '22222222-2222-2222-2222-222222222222', 'CA', NULL, SYSUTCDATETIME());

INSERT INTO ShippingMethods (
    Id,
    TenantId,
    ShippingZoneId,
    Name,
    Description,
    MethodType,
    RateConditionType,
    Currency,
    FlatRate,
    MinimumOrderTotal,
    MaximumOrderTotal,
    IsEnabled,
    CarrierKey,
    CarrierServiceLevel,
    IntegrationSettingsJson,
    EstimatedTransitMinDays,
    EstimatedTransitMaxDays,
    CreatedAt)
VALUES
    ('55555555-5555-5555-5555-555555555555', @TenantId, '22222222-2222-2222-2222-222222222222', 'Ground', 'Ground shipping within North America', 0, 1, 'USD', 9.99, NULL, NULL, 1, NULL, NULL, NULL, 4, 7, SYSUTCDATETIME()),
    ('66666666-6666-6666-6666-666666666666', @TenantId, '22222222-2222-2222-2222-222222222222', 'Express', 'Express 2-day shipping', 0, 1, 'USD', 24.99, NULL, NULL, 1, NULL, NULL, NULL, 2, 3, SYSUTCDATETIME());

-- Europe zone with weight-based rate table
INSERT INTO ShippingZones (Id, TenantId, Name, IsDefault, CreatedAt)
VALUES ('77777777-7777-7777-7777-777777777777', @TenantId, 'Europe', 0, SYSUTCDATETIME());

INSERT INTO ShippingZoneRegions (Id, TenantId, ShippingZoneId, CountryCode, StateCode, CreatedAt)
VALUES
    ('88888888-8888-8888-8888-888888888888', @TenantId, '77777777-7777-7777-7777-777777777777', 'DE', NULL, SYSUTCDATETIME()),
    ('99999999-9999-9999-9999-999999999999', @TenantId, '77777777-7777-7777-7777-777777777777', 'FR', NULL, SYSUTCDATETIME());

INSERT INTO ShippingMethods (
    Id,
    TenantId,
    ShippingZoneId,
    Name,
    Description,
    MethodType,
    RateConditionType,
    Currency,
    FlatRate,
    MinimumOrderTotal,
    MaximumOrderTotal,
    IsEnabled,
    CarrierKey,
    CarrierServiceLevel,
    IntegrationSettingsJson,
    EstimatedTransitMinDays,
    EstimatedTransitMaxDays,
    CreatedAt)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', @TenantId, '77777777-7777-7777-7777-777777777777', 'EU Priority', 'Priority shipping with weight tiers', 2, 2, 'EUR', NULL, NULL, NULL, 1, NULL, NULL, NULL, 3, 5, SYSUTCDATETIME());

INSERT INTO ShippingRateTableEntries (Id, TenantId, ShippingMethodId, MinValue, MaxValue, Rate, CreatedAt)
VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', @TenantId, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 0, 2, 14.99, SYSUTCDATETIME()),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', @TenantId, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 2, 5, 24.99, SYSUTCDATETIME()),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', @TenantId, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 5, NULL, 39.99, SYSUTCDATETIME());
