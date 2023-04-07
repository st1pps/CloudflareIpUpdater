using System.ComponentModel.DataAnnotations;

namespace Stipps.CloudflareIpUpdates.DynDnsEndpoint;

public class DynDnsCredentials
{
    public const string SectionName = "DynDnsCredentials";
    
    [Required(AllowEmptyStrings = false)]
    public required string Password { get; init; }
    
    [Required(AllowEmptyStrings = false)]
    public required string Username { get; init; }
}