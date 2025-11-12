using FluentValidation;
using System.Text.RegularExpressions;

namespace ApiTemplate.Security
{
    /// <summary>
    /// SECURITY: Comprehensive password policy validation
    /// Enforces strong password requirements to protect against brute force attacks
    ///
    /// NOTE: Password policies are now stored in the database (TblPasswordPolicy table).
    /// These static settings serve as fallback defaults and are dynamically updated
    /// by IPasswordPolicyService based on company-specific database configurations.
    ///
    /// To use database-driven policies:
    ///   1. Use IPasswordPolicyService.ValidatePasswordAsync(password, companyId)
    ///   2. Or call IPasswordPolicyService.ApplyPolicySettingsAsync(companyId) first
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>
        /// Runtime password policy settings.
        /// These are populated from database by IPasswordPolicyService.
        /// Default values are used as fallback if no database policy exists.
        /// </summary>
        public static class Settings
        {
            // IMPORTANT: These are runtime settings populated from TblPasswordPolicy table
            // Default values below are used only as fallback
            public static int MinimumLength { get; set; } = 12;
            public static int MaximumLength { get; set; } = 128;
            public static bool RequireUppercase { get; set; } = true;
            public static bool RequireLowercase { get; set; } = true;
            public static bool RequireDigit { get; set; } = true;
            public static bool RequireSpecialCharacter { get; set; } = true;
            public static int MinimumUniqueCharacters { get; set; } = 5;
            public static bool ProhibitCommonPasswords { get; set; } = true;
            public static bool ProhibitSequentialCharacters { get; set; } = true;
            public static bool ProhibitRepeatingCharacters { get; set; } = true;
        }

        /// <summary>
        /// Validates password against security policy
        /// </summary>
        public static PasswordValidationResult Validate(string password)
        {
            var result = new PasswordValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is required");
                return result;
            }

            // Length validation
            if (password.Length < Settings.MinimumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must be at least {Settings.MinimumLength} characters long");
            }

            if (password.Length > Settings.MaximumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must not exceed {Settings.MaximumLength} characters");
            }

            // Character type requirements
            if (Settings.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter (A-Z)");
            }

            if (Settings.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter (a-z)");
            }

            if (Settings.RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit (0-9)");
            }

            if (Settings.RequireSpecialCharacter && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");
            }

            // Unique characters check
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars < Settings.MinimumUniqueCharacters)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must contain at least {Settings.MinimumUniqueCharacters} unique characters");
            }

            // Common password check
            if (Settings.ProhibitCommonPasswords && IsCommonPassword(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is too common. Please choose a more unique password");
            }

            // Sequential characters check (e.g., "123", "abc")
            if (Settings.ProhibitSequentialCharacters && ContainsSequentialCharacters(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password must not contain sequential characters (e.g., '123', 'abc')");
            }

            // Repeating characters check (e.g., "aaa", "111")
            if (Settings.ProhibitRepeatingCharacters && ContainsRepeatingCharacters(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password must not contain more than 2 repeating characters in a row");
            }

            // Calculate password strength
            result.Strength = CalculatePasswordStrength(password);

            return result;
        }

        /// <summary>
        /// Checks if password contains sequential characters
        /// </summary>
        private static bool ContainsSequentialCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                // Check for sequential numbers (123, 234, etc.)
                if (char.IsDigit(password[i]) && char.IsDigit(password[i + 1]) && char.IsDigit(password[i + 2]))
                {
                    if (password[i + 1] == password[i] + 1 && password[i + 2] == password[i + 1] + 1)
                        return true;
                    if (password[i + 1] == password[i] - 1 && password[i + 2] == password[i + 1] - 1)
                        return true;
                }

                // Check for sequential letters (abc, xyz, etc.)
                if (char.IsLetter(password[i]) && char.IsLetter(password[i + 1]) && char.IsLetter(password[i + 2]))
                {
                    var lower = password.ToLower();
                    if (lower[i + 1] == lower[i] + 1 && lower[i + 2] == lower[i + 1] + 1)
                        return true;
                    if (lower[i + 1] == lower[i] - 1 && lower[i + 2] == lower[i + 1] - 1)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if password contains repeating characters
        /// </summary>
        private static bool ContainsRepeatingCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks against common password list
        /// </summary>
        private static bool IsCommonPassword(string password)
        {
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "password123", "123456", "12345678", "123456789",
                "qwerty", "abc123", "monkey", "letmein", "trustno1",
                "dragon", "baseball", "iloveyou", "master", "sunshine",
                "ashley", "bailey", "passw0rd", "shadow", "superman",
                "qazwsx", "michael", "football", "welcome", "jesus",
                "ninja", "mustang", "password1", "admin", "administrator",
                "root", "toor", "pass", "test", "guest", "info", "adm",
                "mysql", "user", "administrator", "oracle", "ftp", "pi",
                "puppet", "ansible", "ec2-user", "vagrant", "azureuser"
            };

            return commonPasswords.Contains(password.ToLower());
        }

        /// <summary>
        /// Calculates password strength on a scale of 0-100
        /// </summary>
        private static int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            // Length points (up to 40 points)
            strength += Math.Min(password.Length * 2, 40);

            // Character variety points (up to 40 points)
            if (Regex.IsMatch(password, @"[a-z]")) strength += 10;
            if (Regex.IsMatch(password, @"[A-Z]")) strength += 10;
            if (Regex.IsMatch(password, @"[0-9]")) strength += 10;
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) strength += 10;

            // Unique characters (up to 20 points)
            var uniqueChars = password.Distinct().Count();
            strength += Math.Min(uniqueChars * 2, 20);

            // Deduct points for common patterns
            if (IsCommonPassword(password)) strength -= 30;
            if (ContainsSequentialCharacters(password)) strength -= 10;
            if (ContainsRepeatingCharacters(password)) strength -= 10;

            return Math.Max(0, Math.Min(100, strength));
        }

        /// <summary>
        /// Gets a user-friendly strength description
        /// </summary>
        public static string GetStrengthDescription(int strength)
        {
            return strength switch
            {
                >= 80 => "Very Strong",
                >= 60 => "Strong",
                >= 40 => "Moderate",
                >= 20 => "Weak",
                _ => "Very Weak"
            };
        }
    }

    /// <summary>
    /// Result of password validation
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int Strength { get; set; }
        public string StrengthDescription => PasswordPolicy.GetStrengthDescription(Strength);
    }

    /// <summary>
    /// FluentValidation extension for password validation
    /// </summary>
    public static class PasswordValidatorExtensions
    {
        public static IRuleBuilderInitial<T, string> MustBeStrongPassword<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return (IRuleBuilderInitial<T, string>)ruleBuilder.Custom((password, context) =>
            {
                var result = PasswordPolicy.Validate(password);
                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        context.AddFailure(error);
                    }
                }
            });
        }
    }
}
