/*
  Trend Analysis report used by /GMCCalculatorDetails/TrendAnalysis.
  Returns seven result sets:
    0 Trend metrics, one row per UW year (view renders transposed)
    1 Insurer / TPA / Broker per UW year
    2 Relationship-wise claims per UW year
    3 Disease-category claims per UW year
    4 Relationship-wise lives (Female/Male/Total/%Mix) from the enrollment table
    5 Available UW years for the policy (for the FY filter dropdown)
    6 All policy numbers (for the policy dropdown)
  @FYYear filters result sets 0-3 to a single UW year (e.g. '2024-25').
  @PolicyNo is optional: when NULL the report aggregates every policy that
  has uploaded enrollment/claim data (FY-wise view); when supplied the
  report is limited to that policy (policy-wise view).
*/
CREATE OR ALTER PROCEDURE dbo.udsp_GetGMC_TrendAnalysis
    @PolicyNo      varchar(100) = NULL,
    @FYYear        varchar(20)  = NULL,
    @TpaFeesPct    decimal(9,2) = 3.5,
    @BrokeragePct  decimal(9,2) = 7.5
AS
BEGIN
    SET NOCOUNT ON;

    SET @PolicyNo = NULLIF(LTRIM(RTRIM(ISNULL(@PolicyNo, ''))), '');
    SET @FYYear   = NULLIF(LTRIM(RTRIM(ISNULL(@FYYear, ''))), '');

    -- Skip the report queries entirely until a policy or FY is chosen.
    DECLARE @BuildReport bit =
        CASE WHEN @PolicyNo IS NOT NULL OR @FYYear IS NOT NULL THEN 1 ELSE 0 END;

    -- Policies in scope: the selected one, or every policy with uploaded data.
    SELECT PolicyNo
    INTO #scope
    FROM (
        SELECT DISTINCT PolicyNo_unique PolicyNo FROM dbo.tbl_GMC_Enrollment_Data WITH (NOLOCK)
        UNION
        SELECT DISTINCT PolicyNo_unique FROM dbo.tbl_GMC_Claim_Data_new WITH (NOLOCK)
    ) p
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(PolicyNo, ''))), '') IS NOT NULL
      AND (@PolicyNo IS NULL OR PolicyNo = @PolicyNo);

    ------------------------------------------------------------------
    -- Quote versions per UW year (latest version inside each year)
    ------------------------------------------------------------------
    SELECT x.*,
        CASE WHEN MONTH(x.sd) >= 4
             THEN CONCAT(YEAR(x.sd), '-', RIGHT(CAST(YEAR(x.sd) + 1 AS varchar(4)), 2))
             ELSE CONCAT(YEAR(x.sd) - 1, '-', RIGHT(CAST(YEAR(x.sd) AS varchar(4)), 2))
        END UWYear
    INTO #quotes
    FROM (
        SELECT q.*,
               COALESCE(TRY_CONVERT(date, LEFT(q.PolicyStartDate, 10), 105),
                        TRY_CONVERT(date, LEFT(q.PolicyStartDate, 10), 103),
                        TRY_CONVERT(date, LEFT(q.PolicyStartDate, 10), 101),
                        TRY_CONVERT(date, q.PolicyStartDate)) sd
        FROM dbo.tbl_GMC_QuoteVersionDetails q WITH (NOLOCK)
        WHERE @BuildReport = 1
          AND EXISTS (SELECT 1 FROM #scope s WHERE s.PolicyNo = q.PolicyNO)
    ) x
    WHERE sd IS NOT NULL
      AND sd >= '2000-01-01' AND sd <= DATEADD(year, 2, GETDATE());

    DECLARE @FallbackSd date =
        COALESCE((SELECT MAX(sd) FROM #quotes), CONVERT(date, GETDATE()));

    DECLARE @CurrentYear varchar(20) =
        CASE WHEN MONTH(@FallbackSd) >= 4
             THEN CONCAT(YEAR(@FallbackSd), '-', RIGHT(CAST(YEAR(@FallbackSd) + 1 AS varchar(4)), 2))
             ELSE CONCAT(YEAR(@FallbackSd) - 1, '-', RIGHT(CAST(YEAR(@FallbackSd) AS varchar(4)), 2))
        END;

    ------------------------------------------------------------------
    -- Claims normalised: UW year, amounts, status group, relation group
    ------------------------------------------------------------------
    SELECT
        CASE WHEN MONTH(x.sd) >= 4
             THEN CONCAT(YEAR(x.sd), '-', RIGHT(CAST(YEAR(x.sd) + 1 AS varchar(4)), 2))
             ELSE CONCAT(YEAR(x.sd) - 1, '-', RIGHT(CAST(YEAR(x.sd) AS varchar(4)), 2))
        END UWYear,
        x.IncAmt,
        x.StatusGroup,
        CASE
            WHEN x.rel LIKE '%self%' OR x.rel IN ('employee','emp') THEN 'Employee'
            WHEN x.rel LIKE '%spouse%' OR x.rel LIKE '%wife%' OR x.rel LIKE '%husband%' THEN 'Spouse'
            WHEN x.rel LIKE '%child%' OR x.rel LIKE '%son%' OR x.rel LIKE '%daughter%' THEN 'Children'
            WHEN x.rel LIKE '%father%' OR x.rel LIKE '%mother%' OR x.rel LIKE '%parent%' THEN 'Parents'
            ELSE 'Others'
        END RelationGroup,
        ISNULL(NULLIF(LTRIM(RTRIM(x.ICD_Group)), ''), 'Others') DiseaseCategory,
        x.Insurance_Company_Name,
        x.TPA
    INTO #claims
    FROM (
        SELECT c.*,
               /* Dates before 2000 (Excel epoch / junk) fall back to the policy year. */
               CASE WHEN raw.sd IS NULL
                      OR raw.sd < '2000-01-01'
                      OR raw.sd > DATEADD(year, 2, GETDATE())
                    THEN @FallbackSd ELSE raw.sd END sd,
               COALESCE(TRY_CONVERT(decimal(18,2), NULLIF(c.IncurredAmount, '')),
                        TRY_CONVERT(decimal(18,2), NULLIF(c.ClaimedAmount, '')), 0) IncAmt,
               LOWER(LTRIM(RTRIM(ISNULL(c.Relation, '')))) rel,
               ISNULL((SELECT TOP (1) m.mastercolumn
                       FROM dbo.tbl_GMC_Paid_Master m WITH (NOLOCK)
                       WHERE m.mastercolumn IN ('Paid','Outstanding','Closed','Rejected')
                         AND LTRIM(RTRIM(m.Defination)) <> ''
                         AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(ISNULL(c.ClaimStatus, '')))
                       ORDER BY CASE m.mastercolumn
                                    WHEN 'Paid' THEN 1 WHEN 'Outstanding' THEN 2
                                    WHEN 'Closed' THEN 3 ELSE 4 END),
                      'Outstanding') StatusGroup
        FROM dbo.tbl_GMC_Claim_Data_new c WITH (NOLOCK)
        CROSS APPLY (
            SELECT COALESCE(TRY_CONVERT(date, LEFT(c.PolicyStartDate, 10), 103),
                            TRY_CONVERT(date, LEFT(c.PolicyStartDate, 10), 105),
                            TRY_CONVERT(date, LEFT(c.PolicyStartDate, 10), 101),
                            TRY_CONVERT(date, c.PolicyStartDate)) sd
        ) raw
        WHERE @BuildReport = 1
          AND EXISTS (SELECT 1 FROM #scope s WHERE s.PolicyNo = c.PolicyNo_unique)
    ) x;

    ------------------------------------------------------------------
    -- Year universe + per-year quote figures
    ------------------------------------------------------------------
    SELECT UWYear INTO #years FROM #claims GROUP BY UWYear
    UNION
    SELECT UWYear FROM #quotes GROUP BY UWYear;

    -- Latest quote version per policy inside each year, then summed per year
    -- so the FY-wise (all policies) view aggregates premiums and lives.
    SELECT UWYear,
        SUM(inception_premium) InceptionPremium,
        SUM(FinalYearPremium)  EndPremium,
        SUM(OpeningLives)      InceptionLives,
        SUM(ClosingLives)      EndLives,
        MAX(Insurance_Company_Name) QuoteInsurer,
        MAX(TPA) QuoteTPA
    INTO #quoteAgg
    FROM (
        SELECT q.*,
               ROW_NUMBER() OVER (PARTITION BY q.PolicyNO, q.UWYear
                                  ORDER BY q.insertdate DESC, q.id DESC) rn
        FROM #quotes q
    ) latest
    WHERE rn = 1
    GROUP BY UWYear;

    SELECT
        y.UWYear,
        q.InceptionPremium,
        q.EndPremium,
        q.InceptionLives,
        q.EndLives,
        CAST((ISNULL(q.InceptionLives, 0) + ISNULL(q.EndLives, 0)) / 2.0 AS decimal(18,0)) WtdAvgLives,
        cl.ClaimsWithIBNR,
        cl.NoOfClaimsWithIBNR,
        cl.PaidClaimCount,
        q.QuoteInsurer,
        q.QuoteTPA
    INTO #metrics
    FROM #years y
    LEFT JOIN #quoteAgg q ON q.UWYear = y.UWYear
    OUTER APPLY (
        SELECT
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN c.IncAmt ELSE 0 END) ClaimsWithIBNR,
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN 1 ELSE 0 END) NoOfClaimsWithIBNR,
            SUM(CASE WHEN c.StatusGroup = 'Paid' THEN 1 ELSE 0 END) PaidClaimCount
        FROM #claims c
        WHERE c.UWYear = y.UWYear
    ) cl;

    ------------------------------------------------------------------
    -- 0: Trend metrics with derived ratios
    --    (Inflation is computed across all years, then filtered)
    ------------------------------------------------------------------
    ;WITH Derived AS
    (
        SELECT *,
            CAST(100.0 * ClaimsWithIBNR / NULLIF(EndPremium, 0) AS decimal(18,1)) LossRatio,
            CAST(ClaimsWithIBNR / NULLIF(NoOfClaimsWithIBNR, 0) AS decimal(18,0)) ACS,
            CAST(100.0 * NoOfClaimsWithIBNR / NULLIF(WtdAvgLives, 0) AS decimal(18,1)) IR,
            CAST(ClaimsWithIBNR / NULLIF(WtdAvgLives, 0) AS decimal(18,0)) RiskRate,
            CAST(InceptionPremium / NULLIF(InceptionLives, 0) AS decimal(18,0)) PremiumPerLife
        FROM #metrics
    ),
    Final AS
    (
        SELECT *,
            CAST(CASE WHEN LAG(RiskRate) OVER (ORDER BY UWYear) IS NULL
                        OR LAG(RiskRate) OVER (ORDER BY UWYear) = 0 THEN NULL
                      ELSE 100.0 * (RiskRate - LAG(RiskRate) OVER (ORDER BY UWYear))
                           / LAG(RiskRate) OVER (ORDER BY UWYear) END AS decimal(18,1)) Inflation
        FROM Derived
    )
    SELECT
        UWYear, InceptionPremium, EndPremium, InceptionLives, EndLives, WtdAvgLives,
        LossRatio,
        @TpaFeesPct   TpaFeesPct,
        @BrokeragePct BrokeragePct,
        CAST(ISNULL(LossRatio, 0) + @TpaFeesPct + @BrokeragePct AS decimal(18,1)) LRInclTpaBrokerage,
        NoOfClaimsWithIBNR, ClaimsWithIBNR, ACS, IR, RiskRate, Inflation, PremiumPerLife
    FROM Final
    WHERE (@FYYear IS NULL OR UWYear = @FYYear)
    ORDER BY UWYear;

    ------------------------------------------------------------------
    -- 1: Insurer / TPA / Broker per UW year
    ------------------------------------------------------------------
    SELECT
        m.UWYear,
        COALESCE(NULLIF(MAX(c.Insurance_Company_Name), ''), MAX(m.QuoteInsurer), '') InsurerName,
        COALESCE(NULLIF(MAX(c.TPA), ''), MAX(m.QuoteTPA), '') TpaName,
        '' BrokerName
    FROM #metrics m
    LEFT JOIN #claims c ON c.UWYear = m.UWYear
    WHERE (@FYYear IS NULL OR m.UWYear = @FYYear)
    GROUP BY m.UWYear
    ORDER BY m.UWYear;

    ------------------------------------------------------------------
    -- 2: Relationship-wise claims per UW year
    ------------------------------------------------------------------
    ;WITH Rel AS
    (
        SELECT c.UWYear, c.RelationGroup,
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN c.IncAmt ELSE 0 END) IncurredAmount,
            SUM(CASE WHEN c.StatusGroup = 'Paid' THEN 1 ELSE 0 END) ClaimsCount,
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN 1 ELSE 0 END) ClaimsCountWithIBNR
        FROM #claims c
        WHERE (@FYYear IS NULL OR c.UWYear = @FYYear)
        GROUP BY c.UWYear, c.RelationGroup
    )
    SELECT r.UWYear, r.RelationGroup, r.IncurredAmount, r.ClaimsCount, r.ClaimsCountWithIBNR,
        CAST(r.IncurredAmount / NULLIF(r.ClaimsCountWithIBNR, 0) AS decimal(18,0)) ACS,
        CAST(100.0 * r.ClaimsCountWithIBNR / NULLIF(m.WtdAvgLives, 0) AS decimal(18,1)) IR,
        CASE r.RelationGroup WHEN 'Employee' THEN 1 WHEN 'Spouse' THEN 2
             WHEN 'Children' THEN 3 WHEN 'Parents' THEN 4 ELSE 5 END SortOrder
    FROM Rel r
    LEFT JOIN #metrics m ON m.UWYear = r.UWYear
    UNION ALL
    SELECT r.UWYear, 'Overall', SUM(r.IncurredAmount), SUM(r.ClaimsCount), SUM(r.ClaimsCountWithIBNR),
        CAST(SUM(r.IncurredAmount) / NULLIF(SUM(r.ClaimsCountWithIBNR), 0) AS decimal(18,0)),
        CAST(100.0 * SUM(r.ClaimsCountWithIBNR) / NULLIF(MAX(m.WtdAvgLives), 0) AS decimal(18,1)),
        99
    FROM Rel r
    LEFT JOIN #metrics m ON m.UWYear = r.UWYear
    GROUP BY r.UWYear
    ORDER BY UWYear DESC, SortOrder;

    ------------------------------------------------------------------
    -- 3: Disease-category claims per UW year
    ------------------------------------------------------------------
    ;WITH Dis AS
    (
        SELECT c.UWYear, c.DiseaseCategory,
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN c.IncAmt ELSE 0 END) IncurredAmount,
            SUM(CASE WHEN c.StatusGroup = 'Paid' THEN 1 ELSE 0 END) ClaimsCount,
            SUM(CASE WHEN c.StatusGroup IN ('Paid','Outstanding') THEN 1 ELSE 0 END) ClaimsCountWithIBNR
        FROM #claims c
        WHERE (@FYYear IS NULL OR c.UWYear = @FYYear)
        GROUP BY c.UWYear, c.DiseaseCategory
    )
    SELECT d.UWYear, d.DiseaseCategory, d.IncurredAmount, d.ClaimsCount, d.ClaimsCountWithIBNR,
        CAST(d.IncurredAmount / NULLIF(d.ClaimsCountWithIBNR, 0) AS decimal(18,0)) ACS,
        CAST(100.0 * d.ClaimsCountWithIBNR / NULLIF(m.WtdAvgLives, 0) AS decimal(18,1)) IR,
        CASE WHEN d.DiseaseCategory = 'Others' THEN 98 ELSE 1 END SortOrder
    FROM Dis d
    LEFT JOIN #metrics m ON m.UWYear = d.UWYear
    UNION ALL
    SELECT d.UWYear, 'Overall', SUM(d.IncurredAmount), SUM(d.ClaimsCount), SUM(d.ClaimsCountWithIBNR),
        CAST(SUM(d.IncurredAmount) / NULLIF(SUM(d.ClaimsCountWithIBNR), 0) AS decimal(18,0)),
        CAST(100.0 * SUM(d.ClaimsCountWithIBNR) / NULLIF(MAX(m.WtdAvgLives), 0) AS decimal(18,1)),
        99
    FROM Dis d
    LEFT JOIN #metrics m ON m.UWYear = d.UWYear
    GROUP BY d.UWYear
    ORDER BY UWYear DESC, SortOrder, IncurredAmount DESC;

    ------------------------------------------------------------------
    -- 4: Relationship-wise lives from the enrollment table
    --    (enrollment has no year column, so this reflects the current
    --     enrollment of the policy, labelled with the current UW year)
    ------------------------------------------------------------------
    ;WITH Enroll AS
    (
        SELECT
            CASE
                WHEN rel LIKE '%self%' OR rel IN ('employee','emp') THEN 'Self'
                WHEN rel LIKE '%spouse%' OR rel LIKE '%wife%' OR rel LIKE '%husband%' THEN 'Spouse'
                WHEN rel LIKE '%child%' OR rel LIKE '%son%' OR rel LIKE '%daughter%' THEN 'Child'
                WHEN rel LIKE '%father%' OR rel LIKE '%mother%' OR rel LIKE '%parent%' THEN 'Parents'
                ELSE 'Other'
            END RelationGroup,
            CASE
                WHEN gen IN ('f','female') THEN 'F'
                WHEN gen IN ('m','male') THEN 'M'
                ELSE ''
            END GenderGroup
        FROM (
            SELECT LOWER(LTRIM(RTRIM(ISNULL(RelationshipwithEmployee, '')))) rel,
                   LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(ISNULL(Gender, ''),
                        CHAR(9), ''), CHAR(13), ''), CHAR(10), '')))) gen
            FROM dbo.tbl_GMC_Enrollment_Data WITH (NOLOCK)
            WHERE @BuildReport = 1
              AND (@PolicyNo IS NULL OR PolicyNo_unique = @PolicyNo)
        ) e
    ),
    Grouped AS
    (
        SELECT RelationGroup,
            SUM(CASE WHEN GenderGroup = 'F' THEN 1 ELSE 0 END) Female,
            SUM(CASE WHEN GenderGroup = 'M' THEN 1 ELSE 0 END) Male,
            COUNT(1) Total
        FROM Enroll
        GROUP BY RelationGroup
    )
    SELECT ISNULL(@FYYear, @CurrentYear) UWYear, RelationGroup, Female, Male, Total,
        CAST(100.0 * Total / NULLIF((SELECT SUM(Total) FROM Grouped), 0) AS decimal(8,1)) MixPct,
        CASE RelationGroup WHEN 'Self' THEN 1 WHEN 'Spouse' THEN 2
             WHEN 'Child' THEN 3 WHEN 'Parents' THEN 4 ELSE 5 END SortOrder
    FROM Grouped
    UNION ALL
    SELECT ISNULL(@FYYear, @CurrentYear), 'Total', SUM(Female), SUM(Male), SUM(Total), CAST(100.0 AS decimal(8,1)), 99
    FROM Grouped
    HAVING COUNT(1) > 0
    ORDER BY SortOrder;

    ------------------------------------------------------------------
    -- 5: Financial-year dropdown values maintained in the master page
    ------------------------------------------------------------------
    SELECT FinancialYear AS UWYear
    FROM dbo.tbl_GMC_FinancialYear_Master WITH (NOLOCK)
    ORDER BY FinancialYear DESC;

    ------------------------------------------------------------------
    -- 6: Policy numbers that have uploaded data (for the policy dropdown)
    ------------------------------------------------------------------
    SELECT PolicyNo FROM (
        SELECT DISTINCT PolicyNo_unique PolicyNo FROM dbo.tbl_GMC_Enrollment_Data WITH (NOLOCK)
        UNION
        SELECT DISTINCT PolicyNo_unique FROM dbo.tbl_GMC_Claim_Data_new WITH (NOLOCK)
    ) p
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(PolicyNo, ''))), '') IS NOT NULL
    ORDER BY PolicyNo;
END;
