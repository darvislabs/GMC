# GMC — Database migration scripts

Run these scripts **once, in numeric order**, against the `GMC` database in SSMS.
All scripts are idempotent — safe to re-run.

| # | Script | Purpose |
|---|---|---|
| 1 | `01_NewRoles.sql` | Adds `SalesPerson` and `Underwriter` rows to `dbo.UserRoleMaster`. |
| 2 | `02_RolloverUpload_Tables.sql` | Creates two new tracking tables: `tbl_GMC_RolloverUpload` (per-file status workflow) and `tbl_GMC_ColumnMapping` (per-upload draft mapping). |

## What we deliberately do NOT create

The pipeline reuses the existing column-mapping infrastructure rather than
creating a parallel one:

| Need | Existing object reused |
|------|------------------------|
| Excel column → master column suggestions | `udsp_GMS_Column_Plotting_enrollment` (Enrollment) / `udsp_GMS_Column_Plotting` (Claim) |
| Saving approved mapping back to master   | `udsp_Save_GMC_Enrollment_MappingDatta` / `udsp_Save_GMC_Claim_MappingDatta` |
| Industry list                            | `tbl_GMC_industry_master` |
| Insurance Company autocomplete           | `tbl_company_list` |
| TPA autocomplete                         | `tbl_TPA_list` |
| Data ingestion targets                   | `tbl_GMC_Enrollment_Data` / `tbl_GMC_Claim_Data_new` |
| Calculator / version history             | `SP_GMC_version_list` + `udsp_GetGMC_*` family |

No new stored procedures are introduced. The new tables are accessed from
C# via inline parameterised SQL inside `RolloverUploadRepo`.

## After running

Verify with:

```sql
USE [GMC];
SELECT * FROM dbo.UserRoleMaster;             -- expect SalesPerson + Underwriter
SELECT TOP 1 * FROM dbo.tbl_GMC_RolloverUpload;
SELECT TOP 1 * FROM dbo.tbl_GMC_ColumnMapping;
```

## Database name caveat

The legacy `MasterScripts.sql` and `UserRegistrationScript.sql` files in the
repo root use `USE [Tele_Dashboard]`, but the application's connection string
targets `Initial Catalog=GMC`. These new scripts all use `USE [GMC]` to match
the running app. If your tables actually live in `Tele_Dashboard`, change the
`USE` statement at the top of each script accordingly.

## Then create your first users in the app

After the scripts apply, log in as Admin and go to **Account → UserRegistration**
to create at least one user per role (`SalesPerson`, `Underwriter`). The role
dropdown is populated from `dbo.UserRoleMaster`.
