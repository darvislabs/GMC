/* =========================================================================
   01_NewRoles.sql
   Adds SalesPerson and Underwriter roles to dbo.UserRoleMaster.
   Run order: 1 of 4
   Safe to re-run (idempotent).
   ========================================================================= */
USE [GMC]
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoleMaster WHERE RoleName = 'SalesPerson')
    INSERT INTO dbo.UserRoleMaster (RoleName) VALUES ('SalesPerson');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoleMaster WHERE RoleName = 'Underwriter')
    INSERT INTO dbo.UserRoleMaster (RoleName) VALUES ('Underwriter');
GO

PRINT '01_NewRoles.sql applied successfully.';
GO
