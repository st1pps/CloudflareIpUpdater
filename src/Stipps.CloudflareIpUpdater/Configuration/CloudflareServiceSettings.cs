using System.ComponentModel.DataAnnotations;

namespace Stipps.CloudflareIpUpdater.Configuration;

public class CloudflareServiceSettings
{
    public const string SectionName = "CloudflareServiceSettings";
    
    [Required(AllowEmptyStrings = false)]
    public required string ZoneId { get; init; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public required string RecordName { get; init; } = null!;

    [Required] 
    public required bool ProxyEnabled { get; init; }
    
    public int UpdateIntervalMinutes { get; init; } = 5;

    public int DnsRecordsCacheMinutes { get; init; } = 180;
}