using System.ComponentModel.DataAnnotations;

namespace Stipps.CloudflareApi.Configuration;

public class CloudflareConnectionSettings
{
    public const string SectionName = "CloudflareConnectionSettings";
    
    [Required(AllowEmptyStrings = false)]
    public string ApiToken { get; set; } = null!;
    
    [Required(AllowEmptyStrings = true)]
    [EmailAddress]
    public string Email { get; set; } = null!;
}