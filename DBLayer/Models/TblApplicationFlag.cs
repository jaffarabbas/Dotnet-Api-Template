using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBLayer.Models
{
    /// <summary>
    /// Application feature flags and configuration settings per company
    /// Supports feature toggles, A/B testing, and dynamic configuration
    /// </summary>
    [Table("tblApplicationFlag")]
    public class TblApplicationFlag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FlagID { get; set; }

        [Required]
        public long CompanyID { get; set; }

        [Required]
        [MaxLength(100)]
        public string FlagName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string FlagValue { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DataType { get; set; } = "String"; // String, Boolean, Integer, Decimal, JSON, CSV

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? PossibleValues { get; set; } // Comma-separated or JSON array

        [MaxLength(100)]
        public string? DefaultValue { get; set; }

        public bool ShowToUser { get; set; } = false;

        [MaxLength(50)]
        public string? Category { get; set; } // Security, Feature, UI, Integration, etc.

        public bool IsActive { get; set; } = true;

        public bool IsReadOnly { get; set; } = false;

        [Range(0, 100)]
        public int? DisplayOrder { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public long? CreatedBy { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [MaxLength(100)]
        public string? ModuleNamespace { get; set; } // For grouping flags by module

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual TblCompany? Company { get; set; }
    }
}
