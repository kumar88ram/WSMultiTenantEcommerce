-- Sample analytics queries for reporting dashboards

-- Daily visits for a tenant
SELECT [Date], VisitCount
FROM DailyAnalyticsSummaries
WHERE TenantId = @TenantId
  AND [Date] BETWEEN @StartDate AND @EndDate
ORDER BY [Date];

-- Sales performance by day including order counts
SELECT [Date], SalesAmount, OrderCount
FROM DailyAnalyticsSummaries
WHERE TenantId = @TenantId
  AND [Date] BETWEEN @StartDate AND @EndDate
ORDER BY [Date];

-- Conversion rate summary
SELECT
    [Date],
    VisitCount,
    OrderCount,
    SalesAmount,
    ConversionRate
FROM DailyAnalyticsSummaries
WHERE TenantId = @TenantId
  AND [Date] BETWEEN @StartDate AND @EndDate
ORDER BY [Date];

-- Raw event sampling for auditing recent activity
SELECT TOP (@Take)
    EventType,
    OccurredAt,
    Amount,
    Metadata
FROM AnalyticsEvents
WHERE TenantId = @TenantId
ORDER BY OccurredAt DESC;
