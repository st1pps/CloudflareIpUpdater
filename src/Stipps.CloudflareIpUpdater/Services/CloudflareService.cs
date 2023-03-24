using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stipps.CloudflareApi;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Requests;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Models;

namespace Stipps.CloudflareIpUpdater.Services;

public class CloudflareService
{
    private const string DnsRecordsListCacheKey = "dns_records";
    
    private readonly ILogger<CloudflareService> _logger;
    private readonly ICloudflareApiClient _client;
    private readonly IOptions<CloudflareServiceSettings> _settings;
    private readonly IMemoryCache _cache;

    public CloudflareService(ILogger<CloudflareService> logger, ICloudflareApiClient client, IOptions<CloudflareServiceSettings> settings, IMemoryCache cache)
    {
        _logger = logger;
        _client = client;
        _settings = settings;
        _cache = cache;
    }

    public async Task UpdateIp(IpValues values)
    {
        if (values.V4 is null && values.V6 is null)
        {
            throw new ArgumentException("At least one IP address must not be null");
        }
        
        _logger.LogInformation("Updating IP address to  v4:{IPv4}/v6:{IPv6}", 
            values.V4?.ToString() ?? "none", values.V6?.ToString() ?? "none");
        
        try
        {
            await UpdateIpImpl(values);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update IP address to  v4:{IPv4}/v6:{IPv6}", 
                values.V4?.ToString() ?? "none", values.V6?.ToString() ?? "none");
            throw;
        }

        _logger.LogInformation("Updated IP address successfully to v4:{IPv4}/v6:{IPv6}", 
            values.V4?.ToString() ?? "none", values.V6?.ToString() ?? "none");
    }

    private async Task UpdateIpImpl(IpValues values)
    {
        if (!_cache.TryGetValue(DnsRecordsListCacheKey, out ICollection<DnsRecord>? records))
        {
            _logger.LogInformation("Retrieving DNS records from Cloudflare");
            var recordsResponse = await _client.GetRecordsForZoneAsync(_settings.Value.ZoneId);
            records = recordsResponse.Where(record => 
                record.Name == $"{_settings.Value.RecordName}.{record.ZoneName}" 
                && record.Type is DnsRecordType.A or DnsRecordType.AAAA).ToList();
            _cache.Set(DnsRecordsListCacheKey, records, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_settings.Value.DnsRecordsCacheMinutes)
            });
        }
        else
        {
            _logger.LogInformation("Using cached DNS records");
        }
        
        var v4Changed = await UpdateRecord(values.V4, DnsRecordType.A, records!);
        var v6Changed = await UpdateRecord(values.V6, DnsRecordType.AAAA, records!);
        if(v4Changed || v6Changed) _cache.Remove(DnsRecordsListCacheKey);
    }

    /// <summary>
    /// Updates the record type to the given IP address. Returns true if a change was made
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="type"></param>
    /// <param name="records"></param>
    /// <returns></returns>
    private async Task<bool> UpdateRecord(IPAddress? ip, DnsRecordType type, ICollection<DnsRecord> records)
    {
        DnsRecord? existingRecord = null;
        var recordsRemoved = false;
        if (records.Any())
        {
            if (ip is null)
            {
                await RemoveRecords(records.Where(e => e.Type == type));
                return true;
            }
            
            var allExisting = records.Where(e => e.Type == type).ToList();
            if (allExisting.Count > 1)
            {
                _logger.LogWarning("Found multiple records of type {Type}, removing all but the first one", type);
                await RemoveRecords(allExisting.Skip(1));
                recordsRemoved = true;
            }

            existingRecord = records.FirstOrDefault(e => e.Type == type);
        }

        if (ip is null) return false;

        if (existingRecord is null)
        {
            _logger.LogInformation("No existing {type} record found, creating new one", type);
            await _client.CreateRecord(
                new CreateDnsRecordRequest(_settings.Value.ZoneId, _settings.Value.RecordName, ip)
                {
                    Proxied = true
                });
            return true;
        }

        if (existingRecord.Content == ip.ToString())
        {
            var version = type == DnsRecordType.A ? "v4" : "v6";
            _logger.LogInformation("IP{type} address is already up to date", version);
            return recordsRemoved;
        }

        _logger.LogInformation("Updating existing record {Record} with content {Content} to {NewContent}",
            existingRecord.Name, existingRecord.Content, ip.ToString());
        await _client.UpdateRecord(new UpdateDnsRecordRequest(existingRecord).WithIpContent(ip));
        return true;
    }

    private async Task RemoveRecords(IEnumerable<DnsRecord> records)
    {
        foreach (var record in records)
        {
            _logger.LogInformation("Removing record {Record} with content {Content}", record.Name, record.Content);
            await _client.DeleteRecord(_settings.Value.ZoneId, record.Id);
        }
    }
}