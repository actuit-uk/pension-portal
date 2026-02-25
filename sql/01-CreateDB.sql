USE [master]
GO

IF DB_ID('PensionPortal') IS NOT NULL
BEGIN
    ALTER DATABASE [PensionPortal] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    DROP DATABASE [PensionPortal]
END
GO

CREATE DATABASE [PensionPortal]
GO

USE [PensionPortal]
GO

CREATE TABLE [dbo].[tblUserInfo](
    [UserID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserName] [varchar](50) NOT NULL,
    [PIN] [int] NOT NULL
)
GO

INSERT INTO tblUserInfo (UserName, PIN) VALUES ('admin', 1234)
INSERT INTO tblUserInfo (UserName, PIN) VALUES ('guest', 1111)
INSERT INTO tblUserInfo (UserName, PIN) VALUES ('demo', 2222)
GO
