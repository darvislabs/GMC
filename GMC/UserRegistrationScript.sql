USE [Tele_Dashboard] -- Assume this based on configured ConnectionString name
GO

-- 1. Create UserRegistration Table
CREATE TABLE [dbo].[UserRegistration] (
    [UserId] INT IDENTITY(1,1) PRIMARY KEY,
    [Username] VARCHAR(100) NOT NULL UNIQUE,
    [Password] VARCHAR(255) NOT NULL,
    [Name] VARCHAR(150) NOT NULL,
    [emailid] VARCHAR(150) NOT NULL,
    [mobile] VARCHAR(20) NULL,
    [address] VARCHAR(500) NULL,
    [usertype] VARCHAR(50) NULL,
    [createdon] DATETIME DEFAULT GETDATE(),
    [createdby] VARCHAR(100) NULL
);
GO

-- 2. Create Insert Stored Procedure
CREATE PROCEDURE [dbo].[SP_InsertUserRegistration]
    @Username VARCHAR(100),
    @Password VARCHAR(255),
    @Name VARCHAR(150),
    @emailid VARCHAR(150),
    @mobile VARCHAR(20),
    @address VARCHAR(500),
    @usertype VARCHAR(50),
    @createdby VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[UserRegistration] (Username, Password, Name, emailid, mobile, address, usertype, createdon, createdby)
    VALUES (@Username, @Password, @Name, @emailid, @mobile, @address, @usertype, GETDATE(), @createdby);
    
    SELECT SCOPE_IDENTITY() AS NewUserId;
END
GO

-- 3. Create Update Stored Procedure
CREATE PROCEDURE [dbo].[SP_UpdateUserRegistration]
    @UserId INT,
    @Username VARCHAR(100),
    @Password VARCHAR(255),
    @Name VARCHAR(150),
    @emailid VARCHAR(150),
    @mobile VARCHAR(20),
    @address VARCHAR(500),
    @usertype VARCHAR(50),
    @updatedby VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[UserRegistration]
    SET 
        Username = @Username,
        Password = @Password,
        Name = @Name,
        emailid = @emailid,
        mobile = @mobile,
        address = @address,
        usertype = @usertype
        -- You can track updatedby/updatedon if you add those columns to the table
    WHERE UserId = @UserId;
END
GO
