namespace Stipps.CloudflareApi.Models;

public class DnsRecord
{
    public required string Content { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DnsRecordType Type { get; init; }
    public required DateTimeOffset CreatedOn { get; init; }
    public string? Comment { get; init; }
    public bool Locked { get; init; }
    public DnsRecordMeta? Meta { get; init; }
    public required DateTimeOffset ModifiedOn { get; init; }
    
    public bool Proxiable { get; init; }
    public bool Proxied { get; init; }
    public string[]? Tags { get; init; }
    public int Ttl { get; init; }
    public required string ZoneId { get; init; }
    public required string ZoneName { get; init; }
}