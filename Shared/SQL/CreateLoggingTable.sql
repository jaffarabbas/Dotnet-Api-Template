-- SQL Server Logging Table Creation Script
-- This table will be auto-created by Serilog, but you can run this manually if needed

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApplicationLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApplicationLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Message] NVARCHAR(MAX) NULL,
        [MessageTemplate] NVARCHAR(MAX) NULL,
        [Level] NVARCHAR(128) NULL,
        [TimeStamp] DATETIME NOT NULL,
        [Exception] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [LogEvent] NVARCHAR(MAX) NULL,
        [UserName] NVARCHAR(100) NULL,
        [IPAddress] NVARCHAR(50) NULL,
        [RequestPath] NVARCHAR(500) NULL,
        [ActionName] NVARCHAR(200) NULL,
        [Application] NVARCHAR(100) NULL,
        CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Index for better query performance
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_TimeStamp] ON [dbo].[ApplicationLogs]
    (
        [TimeStamp] DESC
    );

    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level] ON [dbo].[ApplicationLogs]
    (
        [Level]
    );

    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_UserName] ON [dbo].[ApplicationLogs]
    (
        [UserName]
    )
    WHERE [UserName] IS NOT NULL;

    PRINT 'ApplicationLogs table created successfully';
END
ELSE
BEGIN
    PRINT 'ApplicationLogs table already exists';
END
GO

-- Optional: Create a view for audit logs (logs that start with 'AUDIT:')
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'AuditLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    EXEC('
    CREATE VIEW [dbo].[AuditLogs] AS
    SELECT
        Id,
        Message,
        Level,
        TimeStamp,
        UserName,
        IPAddress,
        RequestPath,
        Properties
    FROM [dbo].[ApplicationLogs]
    WHERE Message LIKE ''AUDIT:%''
    ');

    PRINT 'AuditLogs view created successfully';
END
GO

-- Optional: Create stored procedure to clean old logs
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanOldLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_CleanOldLogs]
        @RetentionDays INT = 90
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @CutoffDate DATETIME = DATEADD(DAY, -@RetentionDays, GETDATE());
        DECLARE @DeletedRows INT;

        DELETE FROM [dbo].[ApplicationLogs]
        WHERE [TimeStamp] < @CutoffDate;

        SET @DeletedRows = @@ROWCOUNT;

        SELECT @DeletedRows AS DeletedRows, @CutoffDate AS CutoffDate;
    END
    ');

    PRINT 'sp_CleanOldLogs stored procedure created successfully';
END
GO
