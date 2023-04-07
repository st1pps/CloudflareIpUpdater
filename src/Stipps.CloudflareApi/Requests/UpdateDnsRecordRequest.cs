using System.Net;
using System.Text.Json.Serialization;
using Stipps.CloudflareApi.Models;

namespace Stipps.CloudflareApi.Requests;

public class UpdateDnsRecordRequest
{
    public UpdateDnsRecordRequest(DnsRecord record)
    {
        ZoneId = record.ZoneId;
        RecordId = record.Id;
        Name = record.Name;
        Content = record.Content;
        Proxied = record.Proxied;
        Type = record.Type;
        Comment = record.Comment;
        Tags = record.Tags?.ToList() ?? new List<string>();
        Ttl = record.Ttl;
    }
    
    [JsonIgnore]
    public string ZoneId { get; }
    
    [JsonIgnore]
    public string RecordId { get; }
    
    public string Name { get; set; }
    
    public string Content { get; private set; }
    
    public bool Proxied { get; set; }
    
    public DnsRecordType Type { get; set; }
    
    public string? Comment { get; set; }

    public List<string> Tags { get; }
    
    public int? Ttl { get; private set; }

    public UpdateDnsRecordRequest WithContent(string content)
    {
        Content = content;
        return this;
    }

    public UpdateDnsRecordRequest WithIpContent(IPAddress address)
    {
        Content = address.ToString();
        return this;
    }

    public UpdateDnsRecordRequest WithComment(string comment)
    {
        Comment = comment;
        return this;
    }
    
    public UpdateDnsRecordRequest WithTtl(int? ttl)
    {
        if (ttl == null)
        {
            Ttl = null;
            return this;
        }

        if (ttl is not 1 or < 60 and > 86400)
        {
            throw new ArgumentException("TTL must be 1 for automatic or between 60 and 86400", nameof(ttl));
        }

        Ttl = ttl;
        return this;
    }
}