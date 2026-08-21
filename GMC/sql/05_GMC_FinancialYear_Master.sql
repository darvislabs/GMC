/*
  Financial-year lookup used by the Trend Analysis report.
  Values follow the YYYY-YY format, for example 2025-26.
*/
IF OBJECT_ID('dbo.tbl_GMC_FinancialYear_Master', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_GMC_FinancialYear_Master
    (
        FinancialYear varchar(7) NOT NULL
            CONSTRAINT PK_tbl_GMC_FinancialYear_Master PRIMARY KEY,
        CreatedOn datetime2(0) NOT NULL
            CONSTRAINT DF_tbl_GMC_FinancialYear_Master_CreatedOn DEFAULT SYSUTCDATETIME()
    );
END;

DECLARE @CurrentStartYear int =
    CASE WHEN MONTH(GETDATE()) >= 4 THEN YEAR(GETDATE()) ELSE YEAR(GETDATE()) - 1 END;

;WITH Years AS
(
    SELECT @CurrentStartYear - 5 AS StartYear
    UNION ALL
    SELECT StartYear + 1
    FROM Years
    WHERE StartYear < @CurrentStartYear + 5
)
INSERT INTO dbo.tbl_GMC_FinancialYear_Master (FinancialYear)
SELECT CONCAT(StartYear, '-', RIGHT(CONVERT(varchar(4), StartYear + 1), 2))
FROM Years y
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.tbl_GMC_FinancialYear_Master m
    WHERE m.FinancialYear =
        CONCAT(y.StartYear, '-', RIGHT(CONVERT(varchar(4), y.StartYear + 1), 2))
)
OPTION (MAXRECURSION 20);
