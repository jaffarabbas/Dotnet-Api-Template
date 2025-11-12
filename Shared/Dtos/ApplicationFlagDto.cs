namespace Shared.Dtos
{
    public class ApplicationFlagDto
    {
        public long FlagID { get; set; }
        public long CompanyID { get; set; }
        public string FlagName { get; set; } = string.Empty;
        public string FlagValue { get; set; } = string.Empty;
        public string? DataType { get; set; }
        public string? Description { get; set; }
        public string? PossibleValues { get; set; }
        public string? DefaultValue { get; set; }
        public bool ShowToUser { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public bool IsReadOnly { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ModuleNamespace { get; set; }
    }

    public class CreateApplicationFlagDto
    {
        public long CompanyID { get; set; }
        public string FlagName { get; set; } = string.Empty;
        public string FlagValue { get; set; } = string.Empty;
        public string? DataType { get; set; } = "String";
        public string? Description { get; set; }
        public string? PossibleValues { get; set; }
        public string? DefaultValue { get; set; }
        public bool ShowToUser { get; set; } = false;
        public string? Category { get; set; }
        public bool IsReadOnly { get; set; } = false;
        public int? DisplayOrder { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ModuleNamespace { get; set; }
    }

    public class UpdateApplicationFlagDto
    {
        public long FlagID { get; set; }
        public string FlagValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool ShowToUser { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class FlagValueDto
    {
        public string FlagName { get; set; } = string.Empty;
        public string FlagValue { get; set; } = string.Empty;
    }

    public class BulkFlagUpdateDto
    {
        public long CompanyID { get; set; }
        public Dictionary<string, string> Flags { get; set; } = new();
    }
}
