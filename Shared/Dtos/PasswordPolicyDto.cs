namespace Shared.Dtos
{
    public class PasswordPolicyDto
    {
        public long PasswordPolicyID { get; set; }
        public long CompanyID { get; set; }
        public int MinimumLength { get; set; }
        public int MaximumLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        public int MinimumUniqueCharacters { get; set; }
        public bool ProhibitCommonPasswords { get; set; }
        public bool ProhibitSequentialCharacters { get; set; }
        public bool ProhibitRepeatingCharacters { get; set; }
        public int? PasswordExpirationDays { get; set; }
        public int? PasswordHistoryCount { get; set; }
        public bool EnablePasswordExpiry { get; set; }
        public int? MaxLoginAttempts { get; set; }
        public int? LockoutDurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }

    public class CreatePasswordPolicyDto
    {
        public long CompanyID { get; set; }
        public int MinimumLength { get; set; } = 12;
        public int MaximumLength { get; set; } = 128;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public int MinimumUniqueCharacters { get; set; } = 5;
        public bool ProhibitCommonPasswords { get; set; } = true;
        public bool ProhibitSequentialCharacters { get; set; } = true;
        public bool ProhibitRepeatingCharacters { get; set; } = true;
        public int? PasswordExpirationDays { get; set; }
        public int? PasswordHistoryCount { get; set; }
        public bool EnablePasswordExpiry { get; set; } = false;
        public int? MaxLoginAttempts { get; set; } = 5;
        public int? LockoutDurationMinutes { get; set; } = 30;
        public string? Description { get; set; }
    }

    public class UpdatePasswordPolicyDto
    {
        public long PasswordPolicyID { get; set; }
        public int MinimumLength { get; set; }
        public int MaximumLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        public int MinimumUniqueCharacters { get; set; }
        public bool ProhibitCommonPasswords { get; set; }
        public bool ProhibitSequentialCharacters { get; set; }
        public bool ProhibitRepeatingCharacters { get; set; }
        public int? PasswordExpirationDays { get; set; }
        public int? PasswordHistoryCount { get; set; }
        public bool EnablePasswordExpiry { get; set; }
        public int? MaxLoginAttempts { get; set; }
        public int? LockoutDurationMinutes { get; set; }
        public string? Description { get; set; }
    }
}
