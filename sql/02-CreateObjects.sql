USE [PensionPortal]
GO

-- Tables
IF OBJECT_ID(N'tblPeople', N'U') IS NOT NULL DROP TABLE tblPeople;
IF OBJECT_ID(N'tblCalculationResult', N'U') IS NOT NULL DROP TABLE tblCalculationResult;
IF OBJECT_ID(N'tblAgeFactor', N'U') IS NOT NULL DROP TABLE tblAgeFactor;
GO

CREATE TABLE [dbo].[tblPeople](
    [PersonID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FirstName] [varchar](50) NOT NULL,
    [Surname] [varchar](50) NOT NULL,
    [DateOfBirth] [datetime] NOT NULL
)
GO

CREATE TABLE [dbo].[tblCalculationResult](
    [ResultID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PersonID] [int] NOT NULL,
    [CalculationDate] [datetime] NOT NULL,
    [AgeInMonths] [int] NOT NULL,
    [FactorValue] [float] NULL,
    [CalcTimeStamp] [datetime] NOT NULL DEFAULT GETDATE()
)
GO

CREATE TABLE [dbo].[tblAgeFactor](
    [AgeInMonths] [int] NOT NULL PRIMARY KEY,
    [FactorValue] [float] NOT NULL
)
GO

-- Stored Procedures
IF OBJECT_ID(N'spGetPeople', N'P') IS NOT NULL DROP PROCEDURE spGetPeople;
IF OBJECT_ID(N'spAddPerson', N'P') IS NOT NULL DROP PROCEDURE spAddPerson;
IF OBJECT_ID(N'spGetPersonByID', N'P') IS NOT NULL DROP PROCEDURE spGetPersonByID;
IF OBJECT_ID(N'spSaveResult', N'P') IS NOT NULL DROP PROCEDURE spSaveResult;
IF OBJECT_ID(N'spGetResults', N'P') IS NOT NULL DROP PROCEDURE spGetResults;
IF OBJECT_ID(N'spGetFactors', N'P') IS NOT NULL DROP PROCEDURE spGetFactors;
IF OBJECT_ID(N'spCheckUserPIN', N'P') IS NOT NULL DROP PROCEDURE spCheckUserPIN;
GO

CREATE PROCEDURE [dbo].[spGetPeople]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PersonID, FirstName, Surname, DateOfBirth
    FROM tblPeople
    ORDER BY Surname, FirstName
END
GO

CREATE PROCEDURE [dbo].[spAddPerson]
    @FirstName varchar(50),
    @Surname varchar(50),
    @DateOfBirth datetime
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tblPeople (FirstName, Surname, DateOfBirth)
    VALUES (@FirstName, @Surname, @DateOfBirth)
END
GO

CREATE PROCEDURE [dbo].[spGetPersonByID]
    @PersonID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PersonID, FirstName, Surname, DateOfBirth
    FROM tblPeople
    WHERE PersonID = @PersonID
END
GO

CREATE PROCEDURE [dbo].[spSaveResult]
    @PersonID int,
    @CalculationDate datetime,
    @AgeInMonths int,
    @FactorValue float = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tblCalculationResult (PersonID, CalculationDate, AgeInMonths, FactorValue)
    VALUES (@PersonID, @CalculationDate, @AgeInMonths, @FactorValue)
END
GO

CREATE PROCEDURE [dbo].[spGetResults]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.ResultID, p.FirstName, p.Surname, p.DateOfBirth,
           r.CalculationDate, r.AgeInMonths, r.FactorValue, r.CalcTimeStamp
    FROM tblCalculationResult r
    INNER JOIN tblPeople p ON r.PersonID = p.PersonID
    ORDER BY r.CalcTimeStamp DESC
END
GO

CREATE PROCEDURE [dbo].[spGetFactors]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AgeInMonths, FactorValue
    FROM tblAgeFactor
    ORDER BY AgeInMonths
END
GO

CREATE PROCEDURE [dbo].[spCheckUserPIN]
    @UserName varchar(50),
    @PIN int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @storedPIN int
    SET @storedPIN = (SELECT PIN FROM tblUserInfo WHERE UserName = @UserName)

    IF @storedPIN IS NOT NULL AND @storedPIN = @PIN
        SELECT @UserName AS UserName, 1 AS Authenticated
    ELSE
        SELECT @UserName AS UserName, 0 AS Authenticated
END
GO

-- Seed: sample people
INSERT INTO tblPeople (FirstName, Surname, DateOfBirth) VALUES ('John', 'Smith', '1965-03-15')
INSERT INTO tblPeople (FirstName, Surname, DateOfBirth) VALUES ('Jane', 'Doe', '1978-11-22')
INSERT INTO tblPeople (FirstName, Surname, DateOfBirth) VALUES ('Robert', 'Jones', '1952-07-01')
INSERT INTO tblPeople (FirstName, Surname, DateOfBirth) VALUES ('Sarah', 'Williams', '1990-01-30')
INSERT INTO tblPeople (FirstName, Surname, DateOfBirth) VALUES ('David', 'Brown', '1945-12-25')
GO

-- Seed: age factors (sample — one entry per 12 months from 0 to 1200)
-- In reality this would be loaded from CSV; these are illustrative values
DECLARE @m int = 0
WHILE @m <= 1200
BEGIN
    INSERT INTO tblAgeFactor (AgeInMonths, FactorValue)
    VALUES (@m, ROUND(1.0 - (@m * 0.0007), 4))
    SET @m = @m + 12
END
GO
