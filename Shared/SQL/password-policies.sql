-- Insert a password policy for Company ID 1
INSERT INTO [dbo].[tblPasswordPolicy] (
    [CompanyID],
    [MinimumLength],
    [MaximumLength],
    [RequireUppercase],
    [RequireLowercase],
    [RequireDigit],
    [RequireSpecialCharacter],
    [MinimumUniqueCharacters],
    [ProhibitCommonPasswords],
    [ProhibitSequentialCharacters],
    [ProhibitRepeatingCharacters],
    [PasswordExpirationDays],
    [PasswordHistoryCount],
    [EnablePasswordExpiry],
    [MaxLoginAttempts],
    [LockoutDurationMinutes],
    [IsActive],
    [CreatedDate],
    [ModifiedDate],
    [CreatedBy],
    [ModifiedBy],
    [Description]
)
VALUES (
    1,                    -- CompanyID
    12,                   -- MinimumLength
    128,                  -- MaximumLength
    1,                    -- RequireUppercase (1 = true, 0 = false)
    1,                    -- RequireLowercase
    1,                    -- RequireDigit
    1,                    -- RequireSpecialCharacter
    5,                    -- MinimumUniqueCharacters
    1,                    -- ProhibitCommonPasswords
    1,                    -- ProhibitSequentialCharacters
    1,                    -- ProhibitRepeatingCharacters
    90,                   -- PasswordExpirationDays (NULL if not using expiry)
    5,                    -- PasswordHistoryCount (NULL if not tracking history)
    0,                    -- EnablePasswordExpiry (0 = disabled)
    5,                    -- MaxLoginAttempts
    30,                   -- LockoutDurationMinutes
    1,                    -- IsActive
    GETUTCDATE(),         -- CreatedDate
    NULL,                 -- ModifiedDate
    NULL,                 -- CreatedBy (user ID who created this)
    NULL,                 -- ModifiedBy
    'Default password policy for company' -- Description
);

select * from tblCompany

insert into tblCompany values(1,'0001','Test',getdate())

select * from tblPasswordPolicy

delete from tblPasswordPolicy 

select * from tblResource

select * from tblPermission
