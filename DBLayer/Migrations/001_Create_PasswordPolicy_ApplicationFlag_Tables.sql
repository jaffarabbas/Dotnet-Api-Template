-- =============================================
-- Migration: Create TblPasswordPolicy and TblApplicationFlag Tables
-- Description: Adds password policy and application flag management tables
-- Author: Security Team
-- Date: 2025-01-15
-- =============================================

-- =============================================
-- 1. Create tblPasswordPolicy Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblPasswordPolicy]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblPasswordPolicy] (
        [PasswordPolicyID] BIGINT IDENTITY(1,1) NOT NULL,
        [CompanyID] BIGINT NOT NULL,
        [MinimumLength] INT NOT NULL DEFAULT 12,
        [MaximumLength] INT NOT NULL DEFAULT 128,
        [RequireUppercase] BIT NOT NULL DEFAULT 1,
        [RequireLowercase] BIT NOT NULL DEFAULT 1,
        [RequireDigit] BIT NOT NULL DEFAULT 1,
        [RequireSpecialCharacter] BIT NOT NULL DEFAULT 1,
        [MinimumUniqueCharacters] INT NOT NULL DEFAULT 5,
        [ProhibitCommonPasswords] BIT NOT NULL DEFAULT 1,
        [ProhibitSequentialCharacters] BIT NOT NULL DEFAULT 1,
        [ProhibitRepeatingCharacters] BIT NOT NULL DEFAULT 1,
        [PasswordExpirationDays] INT NULL,
        [PasswordHistoryCount] INT NULL,
        [EnablePasswordExpiry] BIT NOT NULL DEFAULT 0,
        [MaxLoginAttempts] INT NULL DEFAULT 5,
        [LockoutDurationMinutes] INT NULL DEFAULT 30,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] BIGINT NULL,
        [ModifiedBy] BIGINT NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_tblPasswordPolicy] PRIMARY KEY CLUSTERED ([PasswordPolicyID] ASC),
        CONSTRAINT [FK_tblPasswordPolicy_TblCompany] FOREIGN KEY ([CompanyID])
            REFERENCES [dbo].[TblCompany]([CompanyID]) ON DELETE CASCADE,
        CONSTRAINT [CK_tblPasswordPolicy_MinLength] CHECK ([MinimumLength] >= 6 AND [MinimumLength] <= 128),
        CONSTRAINT [CK_tblPasswordPolicy_MaxLength] CHECK ([MaximumLength] >= 8 AND [MaximumLength] <= 256),
        CONSTRAINT [CK_tblPasswordPolicy_LengthRange] CHECK ([MaximumLength] >= [MinimumLength]),
        CONSTRAINT [CK_tblPasswordPolicy_UniqueChars] CHECK ([MinimumUniqueCharacters] >= 1 AND [MinimumUniqueCharacters] <= 20),
        CONSTRAINT [CK_tblPasswordPolicy_ExpirationDays] CHECK ([PasswordExpirationDays] IS NULL OR ([PasswordExpirationDays] >= 1 AND [PasswordExpirationDays] <= 365)),
        CONSTRAINT [CK_tblPasswordPolicy_HistoryCount] CHECK ([PasswordHistoryCount] IS NULL OR ([PasswordHistoryCount] >= 1 AND [PasswordHistoryCount] <= 24)),
        CONSTRAINT [CK_tblPasswordPolicy_MaxAttempts] CHECK ([MaxLoginAttempts] IS NULL OR ([MaxLoginAttempts] >= 1 AND [MaxLoginAttempts] <= 10)),
        CONSTRAINT [CK_tblPasswordPolicy_LockoutDuration] CHECK ([LockoutDurationMinutes] IS NULL OR ([LockoutDurationMinutes] >= 5 AND [LockoutDurationMinutes] <= 1440))
    );

    PRINT 'Table tblPasswordPolicy created successfully';
END
ELSE
BEGIN
    PRINT 'Table tblPasswordPolicy already exists';
END
GO

-- Create indexes for tblPasswordPolicy
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblPasswordPolicy_CompanyID' AND object_id = OBJECT_ID('tblPasswordPolicy'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_tblPasswordPolicy_CompanyID]
    ON [dbo].[tblPasswordPolicy]([CompanyID] ASC)
    WHERE [IsActive] = 1;

    PRINT 'Index IX_tblPasswordPolicy_CompanyID created successfully';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblPasswordPolicy_IsActive' AND object_id = OBJECT_ID('tblPasswordPolicy'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tblPasswordPolicy_IsActive]
    ON [dbo].[tblPasswordPolicy]([IsActive] ASC)
    INCLUDE ([CompanyID], [MinimumLength], [RequireUppercase], [RequireLowercase], [RequireDigit], [RequireSpecialCharacter]);

    PRINT 'Index IX_tblPasswordPolicy_IsActive created successfully';
END
GO

-- =============================================
-- 2. Create tblApplicationFlag Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblApplicationFlag]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tblApplicationFlag] (
        [FlagID] BIGINT IDENTITY(1,1) NOT NULL,
        [CompanyID] BIGINT NOT NULL,
        [FlagName] NVARCHAR(100) NOT NULL,
        [FlagValue] NVARCHAR(1000) NOT NULL,
        [DataType] NVARCHAR(50) NULL DEFAULT 'String',
        [Description] NVARCHAR(500) NULL,
        [PossibleValues] NVARCHAR(200) NULL,
        [DefaultValue] NVARCHAR(100) NULL,
        [ShowToUser] BIT NOT NULL DEFAULT 0,
        [Category] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsReadOnly] BIT NOT NULL DEFAULT 0,
        [DisplayOrder] INT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] BIGINT NULL,
        [ModifiedBy] BIGINT NULL,
        [EffectiveFrom] DATETIME2 NULL,
        [EffectiveTo] DATETIME2 NULL,
        [ModuleNamespace] NVARCHAR(100) NULL,
        CONSTRAINT [PK_tblApplicationFlag] PRIMARY KEY CLUSTERED ([FlagID] ASC),
        CONSTRAINT [FK_tblApplicationFlag_TblCompany] FOREIGN KEY ([CompanyID])
            REFERENCES [dbo].[TblCompany]([CompanyID]) ON DELETE CASCADE,
        CONSTRAINT [CK_tblApplicationFlag_DisplayOrder] CHECK ([DisplayOrder] IS NULL OR ([DisplayOrder] >= 0 AND [DisplayOrder] <= 100)),
        CONSTRAINT [UQ_tblApplicationFlag_CompanyFlag] UNIQUE ([CompanyID], [FlagName])
    );

    PRINT 'Table tblApplicationFlag created successfully';
END
ELSE
BEGIN
    PRINT 'Table tblApplicationFlag already exists';
END
GO

-- Create indexes for tblApplicationFlag
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblApplicationFlag_CompanyID_IsActive' AND object_id = OBJECT_ID('tblApplicationFlag'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tblApplicationFlag_CompanyID_IsActive]
    ON [dbo].[tblApplicationFlag]([CompanyID] ASC, [IsActive] ASC)
    INCLUDE ([FlagName], [FlagValue], [EffectiveFrom], [EffectiveTo]);

    PRINT 'Index IX_tblApplicationFlag_CompanyID_IsActive created successfully';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblApplicationFlag_Category' AND object_id = OBJECT_ID('tblApplicationFlag'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tblApplicationFlag_Category]
    ON [dbo].[tblApplicationFlag]([CompanyID] ASC, [Category] ASC)
    WHERE [IsActive] = 1;

    PRINT 'Index IX_tblApplicationFlag_Category created successfully';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblApplicationFlag_ShowToUser' AND object_id = OBJECT_ID('tblApplicationFlag'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tblApplicationFlag_ShowToUser]
    ON [dbo].[tblApplicationFlag]([CompanyID] ASC, [ShowToUser] ASC)
    WHERE [IsActive] = 1;

    PRINT 'Index IX_tblApplicationFlag_ShowToUser created successfully';
END
GO

-- =============================================
-- 3. Insert Sample Data
-- =============================================

-- Sample Password Policy for Company ID 1 (if exists)
IF EXISTS (SELECT 1 FROM TblCompany WHERE CompanyID = 1)
   AND NOT EXISTS (SELECT 1 FROM tblPasswordPolicy WHERE CompanyID = 1)
BEGIN
    INSERT INTO tblPasswordPolicy (
        CompanyID, MinimumLength, MaximumLength,
        RequireUppercase, RequireLowercase, RequireDigit, RequireSpecialCharacter,
        MinimumUniqueCharacters, ProhibitCommonPasswords, ProhibitSequentialCharacters, ProhibitRepeatingCharacters,
        EnablePasswordExpiry, MaxLoginAttempts, LockoutDurationMinutes,
        IsActive, CreatedDate, Description
    )
    VALUES (
        1, 12, 128,
        1, 1, 1, 1,
        5, 1, 1, 1,
        0, 5, 30,
        1, GETUTCDATE(), 'Default password policy for company'
    );

    PRINT 'Sample password policy inserted for Company ID 1';
END
GO

-- Sample Application Flags for Company ID 1
IF EXISTS (SELECT 1 FROM TblCompany WHERE CompanyID = 1)
   AND NOT EXISTS (SELECT 1 FROM tblApplicationFlag WHERE CompanyID = 1)
BEGIN
    INSERT INTO tblApplicationFlag (CompanyID, FlagName, FlagValue, DataType, Description, Category, ShowToUser, DefaultValue, PossibleValues, DisplayOrder)
    VALUES
    (1, 'EnableTwoFactorAuth', 'false', 'Boolean', 'Enable two-factor authentication', 'Security', 1, 'false', 'true,false', 1),
    (1, 'SessionTimeoutMinutes', '30', 'Integer', 'Session timeout in minutes', 'Security', 0, '30', '15,30,60,120', 2),
    (1, 'AllowedFileExtensions', '.pdf,.docx,.xlsx', 'CSV', 'Allowed file upload extensions', 'Security', 0, '.pdf,.docx', NULL, 3),
    (1, 'MaxFileUploadSizeMB', '10', 'Integer', 'Maximum file upload size in MB', 'Security', 1, '10', '5,10,25,50,100', 4),
    (1, 'EnableMaintenanceMode', 'false', 'Boolean', 'Enable maintenance mode', 'Feature', 1, 'false', 'true,false', 5),
    (1, 'ThemeColor', '#007bff', 'String', 'Primary theme color', 'UI', 1, '#007bff', NULL, 6),
    (1, 'EnableNotifications', 'true', 'Boolean', 'Enable push notifications', 'Feature', 1, 'true', 'true,false', 7),
    (1, 'APIRateLimitPerMinute', '100', 'Integer', 'API rate limit per minute', 'Security', 0, '100', '50,100,200,500', 8);

    PRINT 'Sample application flags inserted for Company ID 1';
END
GO

-- =============================================
-- 4. Create Helper Views (Optional)
-- =============================================

-- View for active password policies
IF OBJECT_ID('vw_ActivePasswordPolicies', 'V') IS NOT NULL
    DROP VIEW vw_ActivePasswordPolicies;
GO

CREATE VIEW vw_ActivePasswordPolicies
AS
SELECT
    pp.PasswordPolicyID,
    pp.CompanyID,
    c.CompanyName,
    pp.MinimumLength,
    pp.RequireUppercase,
    pp.RequireLowercase,
    pp.RequireDigit,
    pp.RequireSpecialCharacter,
    pp.EnablePasswordExpiry,
    pp.PasswordExpirationDays,
    pp.MaxLoginAttempts,
    pp.LockoutDurationMinutes,
    pp.CreatedDate
FROM tblPasswordPolicy pp
INNER JOIN TblCompany c ON pp.CompanyID = c.CompanyID
WHERE pp.IsActive = 1;
GO

-- View for active application flags
IF OBJECT_ID('vw_ActiveApplicationFlags', 'V') IS NOT NULL
    DROP VIEW vw_ActiveApplicationFlags;
GO

CREATE VIEW vw_ActiveApplicationFlags
AS
SELECT
    af.FlagID,
    af.CompanyID,
    c.CompanyName,
    af.FlagName,
    af.FlagValue,
    af.DataType,
    af.Category,
    af.Description,
    af.ShowToUser,
    af.IsReadOnly,
    af.EffectiveFrom,
    af.EffectiveTo
FROM tblApplicationFlag af
INNER JOIN TblCompany c ON af.CompanyID = c.CompanyID
WHERE af.IsActive = 1
  AND (af.EffectiveFrom IS NULL OR af.EffectiveFrom <= GETUTCDATE())
  AND (af.EffectiveTo IS NULL OR af.EffectiveTo >= GETUTCDATE());
GO

PRINT 'Migration completed successfully!';
PRINT 'Tables created: tblPasswordPolicy, tblApplicationFlag';
PRINT 'Views created: vw_ActivePasswordPolicies, vw_ActiveApplicationFlags';
GO
