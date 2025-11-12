using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBLayer.Models
{
    /// <summary>
    /// Password policy configuration per company
    /// Allows different password requirements for different organizations
    /// </summary>
    [Table("tblPasswordPolicy")]
    public class TblPasswordPolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PasswordPolicyID { get; set; }

        [Required]
        public long CompanyID { get; set; }

        [Required]
        [Range(6, 128)]
        public int MinimumLength { get; set; } = 12;

        [Required]
        [Range(8, 256)]
        public int MaximumLength { get; set; } = 128;

        public bool RequireUppercase { get; set; } = true;

        public bool RequireLowercase { get; set; } = true;

        public bool RequireDigit { get; set; } = true;

        public bool RequireSpecialCharacter { get; set; } = true;

        [Range(1, 20)]
        public int MinimumUniqueCharacters { get; set; } = 5;

        public bool ProhibitCommonPasswords { get; set; } = true;

        public bool ProhibitSequentialCharacters { get; set; } = true;

        public bool ProhibitRepeatingCharacters { get; set; } = true;

        [Range(1, 365)]
        public int? PasswordExpirationDays { get; set; }

        [Range(1, 24)]
        public int? PasswordHistoryCount { get; set; }

        public bool EnablePasswordExpiry { get; set; } = false;

        [Range(1, 10)]
        public int? MaxLoginAttempts { get; set; } = 5;

        [Range(5, 1440)]
        public int? LockoutDurationMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public long? CreatedBy { get; set; }

        public long? ModifiedBy { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual TblCompany? Company { get; set; }
    }
}
