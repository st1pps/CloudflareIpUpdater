using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stipps.CloudflareApi;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Requests;
using Stipps.CloudflareIpUpdater.Configuration;

namespace Stipps.CloudflareIpUpdater.Services;

public class CloudflareService
{
    private readonly ILogger<CloudflareService> _logger;
    private readonly ICloudflareApiClient _client;
    private readonly IOptions<CloudflareServiceSettings> _settings;

    public CloudflareService(ILogger<CloudflareService> logger, ICloudflareApiClient client, IOptions<CloudflareServiceSettings> settings)
    {
        _logger = logger;
        _client = client;
        _settings = settings;
    }

    public async Task UpdateIp(IPAddress? v4, IPAddress? v6)
    {
        if (v4 is null && v6 is null)
        {
            throw new ArgumentException("At least one IP address must not be null");
        }
        
        _logger.LogInformation("Updating IP address to {IPv4}/{IPv6}", v4, v6);
        
        try
        {
            await UpdateIpImpl(v4, v6);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update IP address to {IPv4}/{IPv6}", v4, v6);
            throw;
        }

        _logger.LogInformation("Updated IP address successfully to {IPv4}/{IPv6}", v4, v6);
    }

    private async Task UpdateIpImpl(IPAddress? v4, IPAddress? v6)
    {
        var recordsResponse = await _client.GetRecordsForZoneAsync(_settings.Value.ZoneId);
            var records = recordsResponse.Where(record => 
                record.Name == $"{_settings.Value.RecordName}.{record.ZoneName}" 
                && record.Type is DnsRecordType.A or DnsRecordType.AAAA).ToList();

        await UpdateRecord(v4, DnsRecordType.A, records);
        await UpdateRecord(v6, DnsRecordType.AAAA, records);
    }

    private async Task UpdateRecord(IPAddress? ip, DnsRecordType type, ICollection<DnsRecord> records)
    {
        DnsRecord? existingRecord = null;
        if (records.Any())
        {
            if (ip is null)
            {
                await RemoveRecords(records.Where(e => e.Type == type));
                return;
            }
            
            var allExisting = records.Where(e => e.Type == type).ToList();
            if (allExisting.Count > 1)
            {
                _logger.LogWarning("Found multiple records of type {Type}, removing all but the first one", type);
                await RemoveRecords(allExisting.Skip(1));
            }

            existingRecord = records.FirstOrDefault(e => e.Type == type);
        }

        if (ip is null) return;

        if (existingRecord is null)
        {
            _logger.LogInformation("No existing A record found, creating new one");
            await _client.CreateRecord(ip, _settings.Value.ZoneId, _settings.Value.RecordName,
                _settings.Value.ProxyEnabled);
            return;
        }

        if (existingRecord.Content == ip.ToString())
        {
            _logger.LogInformation("IP address is already up to date");
            return;
        }

        _logger.LogInformation("Updating existing record {Record} with content {Content} to {NewContent}",
            existingRecord.Name, existingRecord.Content, ip.ToString());
        await _client.UpdateRecord(new UpdateDnsRecordRequest(existingRecord).WithIpContent(ip));
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