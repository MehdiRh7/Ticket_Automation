/*
  Ticket AI Orchestrator - SQL Server test database

  Run with SQLCMD mode enabled in SSMS, Azure Data Studio, or sqlcmd.
  IMPORTANT: change ReaderPassword before running outside an isolated local test machine.
*/

:setvar DatabaseName "TicketAutomationTest"
:setvar ReaderLogin "ticket_ai_reader"
:setvar ReaderPassword "LocalTest-ChangeMe-2026!"

USE [master];
GO

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [' + '$(DatabaseName)' + N']');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(ReaderLogin)')
BEGIN
    CREATE LOGIN [$(ReaderLogin)]
        WITH PASSWORD = N'$(ReaderPassword)',
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;
END;
GO

USE [$(DatabaseName)];
GO

IF SCHEMA_ID(N'support') IS NULL
    EXEC(N'CREATE SCHEMA [support] AUTHORIZATION [dbo]');
GO

IF OBJECT_ID(N'[support].[Tickets]', N'U') IS NULL
BEGIN
    CREATE TABLE [support].[Tickets]
    (
        [Id]              BIGINT IDENTITY(1,1) NOT NULL,
        [Title]           NVARCHAR(500) NOT NULL,
        [Description]     NVARCHAR(MAX) NOT NULL,
        [CreatedAt]       DATETIMEOFFSET(0) NOT NULL
            CONSTRAINT [DF_Tickets_CreatedAt] DEFAULT SYSDATETIMEOFFSET(),
        [Status]          NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Tickets_Status] DEFAULT N'Draft',
        [Version]         NVARCHAR(100) NULL,
        [Priority]        TINYINT NOT NULL
            CONSTRAINT [DF_Tickets_Priority] DEFAULT 3,
        [RequestedBy]     NVARCHAR(200) NULL,
        [EnvironmentName] NVARCHAR(100) NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_Tickets_Status]
            CHECK ([Status] IN (N'Draft', N'AI_READY', N'Closed')),
        CONSTRAINT [CK_Tickets_Priority]
            CHECK ([Priority] BETWEEN 1 AND 5)
    );

    CREATE INDEX [IX_Tickets_Status_CreatedAt]
        ON [support].[Tickets] ([Status], [CreatedAt] DESC)
        INCLUDE ([Title], [Version]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(ReaderLogin)')
    CREATE USER [$(ReaderLogin)] FOR LOGIN [$(ReaderLogin)];
GO

GRANT SELECT ON OBJECT::[support].[Tickets] TO [$(ReaderLogin)];
DENY INSERT, UPDATE, DELETE ON OBJECT::[support].[Tickets] TO [$(ReaderLogin)];
GO

IF NOT EXISTS (SELECT 1 FROM [support].[Tickets])
BEGIN
    INSERT INTO [support].[Tickets]
        ([Title], [Description], [Status], [Version], [Priority], [RequestedBy], [EnvironmentName])
    VALUES
    (
        N'نمونه اولیه - هنوز پردازش نشود',
        N'این تیکت فقط برای اطمینان از ساخته‌شدن جدول است. وضعیت آن Draft است و Orchestrator نباید آن را بردارد.',
        N'Draft', NULL, 5, N'Test User', N'Local'
    ),
    (
        N'اصلاح مستندات پروژه آزمایشی',
        N'در فایل README یک بخش با عنوان Agent Test اضافه کن و در آن بنویس این تغییر توسط جریان آزمایشی Ticket AI Orchestrator ایجاد شده است. هیچ فایل اجرایی یا تنظیم امنیتی را تغییر نده.',
        N'Draft', NULL, 5, N'Test User', N'Local'
    );
END;
GO

SELECT [Id], [Title], [Status], [Version], [CreatedAt]
FROM [support].[Tickets]
ORDER BY [Id];
GO

PRINT N'Test database created successfully.';
PRINT N'Change one test ticket to AI_READY only after configuring the Orchestrator.';
GO
