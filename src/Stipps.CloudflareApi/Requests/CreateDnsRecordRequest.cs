using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Stipps.CloudflareApi.Models;

namespace Stipps.CloudflareApi.Requests;

public class CreateDnsRecordRequest
{
    public CreateDnsRecordRequest(string zoneId, string name, IPAddress address)
    {
        Name = name;
        Content = address.ToString();
        Type = address.AddressFamily == AddressFamily.InterNetwork ? DnsRecordType.A : DnsRecordType.AAAA;
        ZoneId = zoneId;
    }

    public CreateDnsRecordRequest(string zoneId, string name, DnsRecordType type, string content)
    {
        Name = name;
        Type = type;
        Content = content;
        ZoneId = zoneId;
    }
    
    [JsonIgnore]
    public string ZoneId { get; }
    
    public string Name { get; }
    
    public DnsRecordType Type { get; }
    
    public string Content { get; }
    
    public bool Proxied { get; set; }
    
    public string? Comment { get; set; }

    public List<string> Tags { get; } = new();
    
    public int? Ttl { get; set; }
}