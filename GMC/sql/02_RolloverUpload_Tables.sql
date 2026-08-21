/* =========================================================================
   02_RolloverUpload_Tables.sql
   Minimum new tables for the Sales -> Underwriter rollover pipeline.
   Everything else (column mapping, master synonym persistence, industry/
   company/TPA lookups, calculator, version history) is delegated to the
   existing tables and stored procedures already shipped with the app.

   Run order: 2 of 2
   Safe to re-run (creates only if absent).
   ========================================================================= */
USE [GMC]
GO

/* ---------- Per-file upload tracking (status workflow + dashboard) -------
   No existing table tracks per-upload status, so this one is unavoidable.
   ------------------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_GMC_RolloverUpload')
BEGIN
    CREATE TABLE dbo.tbl_GMC_RolloverUpload
    (
        UploadId             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PolicyNo             VARCHAR(100)  NOT NULL,
        PolicyName           VARCHAR(255)  NULL,
        InsuranceCompany     VARCHAR(255)  NULL,
        TPA                  VARCHAR(255)  NULL,
        IndustryName         VARCHAR(255)  NULL,
        SubType              VARCHAR(50)   NULL,           -- Main / Parent / Topup
        DataCategory         VARCHAR(50)   NOT NULL DEFAULT 'Enrollment', -- Enrollment / Claim
        FileName             VARCHAR(500)  NOT NULL,
        FilePath             VARCHAR(1000) NOT NULL,
        TotalRows            INT           NULL,
        TotalColumns         INT           NULL,
        Status               VARCHAR(50)   NOT NULL DEFAULT 'Pending',
            -- Pending / MappingRequired / UnderReview / Mapped / Completed / Rejected
        UploadedBy           VARCHAR(100)  NOT NULL,
        UploadedOn           DATETIME      NOT NULL DEFAULT GETDATE(),
        AssignedUnderwriter  VARCHAR(100)  NULL,
        ReviewedBy           VARCHAR(100)  NULL,
        ReviewedOn           DATETIME      NULL,
        MappingConfidenceAvg DECIMAL(5,2)  NULL,
        Remarks              NVARCHAR(MAX) NULL,
        IsActive             BIT           NOT NULL DEFAULT 1
    );

    CREATE INDEX IX_RolloverUpload_Status     ON dbo.tbl_GMC_RolloverUpload(Status);
    CREATE INDEX IX_RolloverUpload_UploadedBy ON dbo.tbl_GMC_RolloverUpload(UploadedBy);
    CREATE INDEX IX_RolloverUpload_PolicyNo   ON dbo.tbl_GMC_RolloverUpload(PolicyNo);
END
GO

/* ---------- Per-upload draft column mapping ------------------------------
   The legacy "master" mapping is stored by udsp_Save_GMC_Enrollment_MappingDatta
   (and the Claim variant) — but that one is GLOBAL, applied to all future
   uploads.  This table is the per-upload draft, what the underwriter is
   currently editing.  Once they Approve, the legacy save SP also runs so
   the master mapping learns the new column.
   ------------------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_GMC_ColumnMapping')
BEGIN
    CREATE TABLE dbo.tbl_GMC_ColumnMapping
    (
        MappingId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UploadId       INT           NOT NULL,
        SourceColumn   VARCHAR(255)  NOT NULL,         -- column header from the Excel
        TargetColumn   VARCHAR(100)  NULL,             -- standard DB column (or NULL = unmapped)
        ConfidencePct  DECIMAL(5,2)  NOT NULL DEFAULT 0,
        IsManual       BIT           NOT NULL DEFAULT 0,
        IsApproved     BIT           NOT NULL DEFAULT 0,
        SuggestedBy    VARCHAR(50)   NULL,             -- LegacyMatch / Fuzzy / Manual / None
        CreatedOn      DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ColumnMapping_Upload
            FOREIGN KEY (UploadId) REFERENCES dbo.tbl_GMC_RolloverUpload(UploadId)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_ColumnMapping_UploadId ON dbo.tbl_GMC_ColumnMapping(UploadId);
END
GO

PRINT '02_RolloverUpload_Tables.sql applied successfully.';
GO
