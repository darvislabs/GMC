/*
  Excel summary used by the GMC Calculator "Download Summary" action.
  Returns eight result sets in the order consumed by CommonBAL.EPPlusExportGmcSummary:
    0 Policy details, 1 Policy features, 2 Relationship-wise lives,
    3 Demographic parameters, 4 Paid claims, 5 Outstanding claims,
    6 IBNR working, 7 Saved burn-cost details.
*/
CREATE OR ALTER PROCEDURE dbo.udsp_GetGMC_DownloadSummary
    @PolicyNo     varchar(100),
    @VersionNumber varchar(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    INTO #Enrollment
    FROM dbo.tbl_GMC_Enrollment_Data WITH (NOLOCK)
    WHERE PolicyNo_unique = @PolicyNo;

    SELECT *
    INTO #Claims
    FROM dbo.tbl_GMC_Claim_Data_new WITH (NOLOCK)
    WHERE PolicyNo_unique = @PolicyNo;

    DECLARE @QuoteId int;
    SELECT TOP (1) @QuoteId = id
    FROM dbo.tbl_GMC_QuoteVersionDetails WITH (NOLOCK)
    WHERE PolicyNO = @PolicyNo
      AND (NULLIF(@VersionNumber, '') IS NULL OR VersionNumber = @VersionNumber)
    ORDER BY insertdate DESC, id DESC;

    DECLARE
        @PolicyName varchar(500) = '',
        @Industry varchar(500) = '',
        @Insurer varchar(500) = '',
        @TPA varchar(500) = '',
        @SubType varchar(100) = '',
        @PolicyStart varchar(100) = '',
        @PolicyEnd varchar(100) = '';

    SELECT
        @PolicyName = ISNULL(MAX(PolicyName), ''),
        @Industry = ISNULL(MAX(Industry_Name), ''),
        @Insurer = ISNULL(MAX(Insurance_Company_Name), ''),
        @TPA = ISNULL(MAX(TPA), ''),
        @SubType = ISNULL(MAX(SubType), '')
    FROM #Enrollment;

    SELECT
        @PolicyStart = ISNULL(PolicyStartDate, @PolicyStart),
        @PolicyEnd = ISNULL(PolicyEndDate, @PolicyEnd),
        @Insurer = COALESCE(NULLIF(Insurance_Company_Name, ''), @Insurer),
        @TPA = COALESCE(NULLIF(TPA, ''), @TPA)
    FROM dbo.tbl_GMC_QuoteVersionDetails WITH (NOLOCK)
    WHERE id = @QuoteId;

    /* 0 - Policy Details */
    SELECT SortOrder, FieldName, FieldValue
    FROM (VALUES
        (1, 'Name of the Client', @PolicyName),
        (2, 'Industry Type', @Industry),
        (3, 'Insurance Company', @Insurer),
        (4, 'TPA', @TPA),
        (5, 'Policy Number', @PolicyNo),
        (6, 'Policy Start Date', @PolicyStart),
        (7, 'Policy End Date', @PolicyEnd)
    ) d(SortOrder, FieldName, FieldValue)
    ORDER BY SortOrder;

    DECLARE
        @Lives int = (SELECT COUNT(1) FROM #Enrollment),
        @Employees int = (SELECT COUNT(1) FROM #Enrollment
            WHERE LOWER(LTRIM(RTRIM(ISNULL(RelationshipwithEmployee, '')))) IN ('self','employee','emp')),
        @Children int = (SELECT COUNT(1) FROM #Enrollment
            WHERE LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%child%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%son%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%daughter%'),
        @Spouses int = (SELECT COUNT(1) FROM #Enrollment
            WHERE LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%spouse%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%wife%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%husband%'),
        @Parents int = (SELECT COUNT(1) FROM #Enrollment
            WHERE LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%father%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%mother%'
               OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%parent%'),
        @AverageAge decimal(18,1),
        @AverageSI decimal(18,0);

    SELECT
        @AverageAge = AVG(TRY_CONVERT(decimal(18,2), NULLIF(Age, ''))),
        @AverageSI = AVG(TRY_CONVERT(decimal(18,2), NULLIF(SumInsured, '')))
    FROM #Enrollment;

    /* 1 - Policy Features */
    SELECT SortOrder, FieldName, FieldValue
    FROM (VALUES
        (1, 'Employer-Employee', CASE WHEN @Employees > 0 THEN 'Employer-Employee' ELSE 'Non Employer-Employee' END),
        (2, 'Policy Type', @SubType),
        (3, 'Family Construct', CONCAT('Employees ', @Employees, ' | Spouse ', @Spouses, ' | Children ', @Children, ' | Parents ', @Parents)),
        (4, 'Average Sum Insured', FORMAT(ISNULL(@AverageSI, 0), 'N0', 'en-IN')),
        (5, 'Average Age', FORMAT(ISNULL(@AverageAge, 0), 'N1')),
        (6, 'Total Lives', FORMAT(@Lives, 'N0', 'en-IN'))
    ) d(SortOrder, FieldName, FieldValue)
    ORDER BY SortOrder;

    /* 2 - Relationship-wise lives */
    ;WITH Normalised AS
    (
        SELECT
            CASE
                WHEN LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%self%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) IN ('employee','emp') THEN 'Self'
                WHEN LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%spouse%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%wife%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%husband%' THEN 'Spouse'
                WHEN LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%child%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%son%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%daughter%' THEN 'Child'
                WHEN LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%father%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%mother%'
                  OR LOWER(ISNULL(RelationshipwithEmployee, '')) LIKE '%parent%' THEN 'Parents'
                ELSE 'Other'
            END RelationGroup,
            CASE
                WHEN LOWER(LTRIM(RTRIM(ISNULL(Gender, '')))) IN ('f','female') THEN 'F'
                WHEN LOWER(LTRIM(RTRIM(ISNULL(Gender, '')))) IN ('m','male') THEN 'M'
                ELSE ''
            END GenderGroup
        FROM #Enrollment
    )
    SELECT
        RelationGroup AS [Relationship wise lives],
        SUM(CASE WHEN GenderGroup = 'F' THEN 1 ELSE 0 END) Female,
        SUM(CASE WHEN GenderGroup = 'M' THEN 1 ELSE 0 END) Male,
        COUNT(1) Total,
        CAST(100.0 * COUNT(1) / NULLIF(@Lives, 0) AS decimal(8,1)) [MixPct]
    FROM Normalised
    GROUP BY RelationGroup
    UNION ALL
    SELECT 'Total',
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL(Gender, '')))) IN ('f','female') THEN 1 ELSE 0 END),
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(ISNULL(Gender, '')))) IN ('m','male') THEN 1 ELSE 0 END),
        COUNT(1), CAST(100.0 AS decimal(8,1))
    FROM #Enrollment;

    /* 3 - Demographic Parameters */
    SELECT SortOrder, FieldName, NumericValue
    FROM (VALUES
        (1, 'Family Size', CAST(1.0 * @Lives / NULLIF(@Employees, 0) AS decimal(18,2))),
        (2, 'Child to Spouse Ratio', CAST(1.0 * @Children / NULLIF(@Spouses, 0) AS decimal(18,2))),
        (3, 'Spouse to Emp Ratio', CAST(1.0 * @Spouses / NULLIF(@Employees, 0) AS decimal(18,2))),
        (4, 'Parental Ratio', CAST(1.0 * @Parents / NULLIF(@Employees, 0) AS decimal(18,2))),
        (5, 'PED Lives', CAST(0 AS decimal(18,2)))
    ) d(SortOrder, FieldName, NumericValue)
    ORDER BY SortOrder;

    SELECT *,
        CASE
            WHEN LOWER(ISNULL(TypeOfClaim, '')) LIKE '%cash%'
              OR LOWER(ISNULL(Claim_type, '')) LIKE '%cash%'
              OR LOWER(ISNULL(TypeOfClaim, '')) LIKE '%preauth%' THEN 'Cashless'
            ELSE 'Reimbursement'
        END SummaryClaimType,
        TRY_CONVERT(decimal(18,2), NULLIF(ClaimedAmount, '')) ClaimAmt,
        COALESCE(TRY_CONVERT(decimal(18,2), NULLIF(PaidAmount, '')),
                 TRY_CONVERT(decimal(18,2), NULLIF(ApprovedAmount, '')),
                 TRY_CONVERT(decimal(18,2), NULLIF(Sanctioned_Amount, '')), 0) PaidAmt,
        COALESCE(TRY_CONVERT(decimal(18,2), NULLIF(IncurredAmount, '')),
                 TRY_CONVERT(decimal(18,2), NULLIF(ClaimedAmount, '')), 0) OSAmt
    INTO #ClaimWork
    FROM #Claims;

    DECLARE
        @TotalPaidAmount decimal(18,2) = (
            SELECT ISNULL(SUM(PaidAmt), 0) FROM #ClaimWork c
            WHERE EXISTS (SELECT 1 FROM dbo.tbl_GMC_Paid_Master m
                          WHERE m.mastercolumn = 'Paid' AND LTRIM(RTRIM(m.Defination)) <> ''
                            AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(c.ClaimStatus)))),
        @TotalPaidCount int = (
            SELECT COUNT(1) FROM #ClaimWork c
            WHERE EXISTS (SELECT 1 FROM dbo.tbl_GMC_Paid_Master m
                          WHERE m.mastercolumn = 'Paid' AND LTRIM(RTRIM(m.Defination)) <> ''
                            AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(c.ClaimStatus))));

    /* 4 - Paid Claims */
    ;WITH Paid AS
    (
        SELECT SummaryClaimType ClaimType,
            ISNULL(SUM(ClaimAmt), 0) ClaimedAmount,
            ISNULL(SUM(PaidAmt), 0) PaidAmount,
            COUNT(1) ClaimCount
        FROM #ClaimWork c
        WHERE EXISTS (SELECT 1 FROM dbo.tbl_GMC_Paid_Master m
                      WHERE m.mastercolumn = 'Paid' AND LTRIM(RTRIM(m.Defination)) <> ''
                        AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(c.ClaimStatus)))
        GROUP BY SummaryClaimType
    )
    SELECT ClaimType, ClaimedAmount, PaidAmount, ClaimCount,
        CAST(100.0 * PaidAmount / NULLIF(@TotalPaidAmount, 0) AS decimal(8,1)) AmountPct,
        CAST(100.0 * ClaimCount / NULLIF(@TotalPaidCount, 0) AS decimal(8,1)) CountPct,
        CAST(PaidAmount / NULLIF(ClaimCount, 0) AS decimal(18,0)) ACS,
        CAST(100.0 * PaidAmount / NULLIF(ClaimedAmount, 0) AS decimal(8,1)) PaidRatio
    FROM Paid
    UNION ALL
    SELECT 'Total', ISNULL(SUM(ClaimedAmount),0), ISNULL(SUM(PaidAmount),0), ISNULL(SUM(ClaimCount),0),
        CAST(100 AS decimal(8,1)), CAST(100 AS decimal(8,1)),
        CAST(SUM(PaidAmount) / NULLIF(SUM(ClaimCount), 0) AS decimal(18,0)),
        CAST(100.0 * SUM(PaidAmount) / NULLIF(SUM(ClaimedAmount), 0) AS decimal(8,1))
    FROM Paid;

    /* 5 - Outstanding Claims */
    ;WITH Outstanding AS
    (
        SELECT SummaryClaimType ClaimType, ISNULL(SUM(OSAmt), 0) OutstandingAmount, COUNT(1) ClaimCount
        FROM #ClaimWork c
        WHERE EXISTS (SELECT 1 FROM dbo.tbl_GMC_Paid_Master m
                      WHERE m.mastercolumn = 'Outstanding' AND LTRIM(RTRIM(m.Defination)) <> ''
                        AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(c.ClaimStatus)))
        GROUP BY SummaryClaimType
        UNION ALL
        SELECT 'Reimb. Closed Claims', ISNULL(SUM(OSAmt), 0), COUNT(1)
        FROM #ClaimWork c
        WHERE SummaryClaimType = 'Reimbursement'
          AND EXISTS (SELECT 1 FROM dbo.tbl_GMC_Paid_Master m
                      WHERE m.mastercolumn = 'Closed' AND LTRIM(RTRIM(m.Defination)) <> ''
                        AND LTRIM(RTRIM(m.Defination)) = LTRIM(RTRIM(c.ClaimStatus)))
    )
    SELECT ClaimType, OutstandingAmount, ClaimCount FROM Outstanding
    UNION ALL
    SELECT 'Total', SUM(OutstandingAmount), SUM(ClaimCount) FROM Outstanding;

    DECLARE
        @IBNRPct decimal(18,2) = ISNULL((SELECT TOP (1) ACS
            FROM dbo.tbl_GMC_QuoteVersion_BurnCost_Details WITH (NOLOCK)
            WHERE PolicyLevelId = @QuoteId AND PertiCular = 'IBNR'), 0),
        @SavedIBNR decimal(18,2) = ISNULL((SELECT TOP (1) TotalAmount
            FROM dbo.tbl_GMC_QuoteVersion_BurnCost_Details WITH (NOLOCK)
            WHERE PolicyLevelId = @QuoteId AND PertiCular = 'IBNR'), 0),
        @TotalReimb decimal(18,2) = (
            SELECT ISNULL(SUM(PaidAmt), 0) FROM #ClaimWork
            WHERE SummaryClaimType = 'Reimbursement'),
        @AvgLag decimal(18,1) = (
            SELECT AVG(CAST(DATEDIFF(day,
                COALESCE(TRY_CONVERT(date, DateofAdmission, 103), TRY_CONVERT(date, DateofAdmission)),
                COALESCE(TRY_CONVERT(date, DateofDischarge, 103), TRY_CONVERT(date, DateofDischarge))) AS decimal(18,1)))
            FROM #ClaimWork
            WHERE SummaryClaimType = 'Reimbursement'
              AND COALESCE(TRY_CONVERT(date, DateofAdmission, 103), TRY_CONVERT(date, DateofAdmission)) IS NOT NULL
              AND COALESCE(TRY_CONVERT(date, DateofDischarge, 103), TRY_CONVERT(date, DateofDischarge)) IS NOT NULL);

    /* 6 - IBNR Working */
    SELECT SortOrder, FieldName, NumericValue
    FROM (VALUES
        (1, 'Reimb. Avg lag', ISNULL(@AvgLag, 0)),
        (2, 'Total Reimb.', @TotalReimb),
        (3, 'IBNR Amt', CASE WHEN @SavedIBNR <> 0 THEN @SavedIBNR ELSE @TotalReimb * @IBNRPct / 100 END),
        (4, 'IBNR %', @IBNRPct)
    ) d(SortOrder, FieldName, NumericValue)
    ORDER BY SortOrder;

    /* 7 - Burn Details, saved against the selected quote version */
    SELECT PertiCular AS Particular, ISNULL(TotalAmount,0) TotalAmount,
           ISNULL(NoOfClaim,0) NoOfClaims, ISNULL(Acs,0) ACS
    FROM dbo.tbl_GMC_QuoteVersion_BurnCost_Details WITH (NOLOCK)
    WHERE PolicyLevelId = @QuoteId
    ORDER BY id;
END;
