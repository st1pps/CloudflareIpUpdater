using System.ComponentModel.DataAnnotations;

namespace Stipps.CloudflareApi.Configuration;

public class CloudflareConnectionSettings
{
    public const string SectionName = "CloudflareConnectionSettings";
    
    [Required(AllowEmptyStrings = false)]
    public string ApiToken { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Email { get; set; } = null!;

    public static void Validate(CloudflareConnectionSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var context = new ValidationContext(settings);
        Validator.ValidateObject(settings, context);
    }
}