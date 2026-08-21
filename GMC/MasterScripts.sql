USE [Tele_Dashboard]
GO

-- 1. Create User Role Master Table
CREATE TABLE [dbo].[UserRoleMaster] (
    [RoleId] INT IDENTITY(1,1) PRIMARY KEY,
    [RoleName] VARCHAR(100) NOT NULL UNIQUE,
    [IsActive] BIT DEFAULT 1,
    [CreatedOn] DATETIME DEFAULT GETDATE(),
    [CreatedBy] VARCHAR(100) NULL
);
GO

-- Insert default roles
INSERT INTO [dbo].[UserRoleMaster] (RoleName) VALUES ('Admin'), ('Uploader'), ('Calculator');
GO

-- 2. Create Form Permission Master Table
CREATE TABLE [dbo].[FormPermissionMaster] (
    [PermissionId] INT IDENTITY(1,1) PRIMARY KEY,
    [RoleId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[UserRoleMaster](RoleId),
    [FormName] VARCHAR(150) NOT NULL,
    [CanView] BIT DEFAULT 0,
    [CanEdit] BIT DEFAULT 0,
    [CreatedOn] DATETIME DEFAULT GETDATE(),
    [CreatedBy] VARCHAR(100) NULL
);
GO
