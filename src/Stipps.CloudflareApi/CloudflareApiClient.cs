using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Requests;

namespace Stipps.CloudflareApi;

public interface ICloudflareApiClient
{
    Task<IEnumerable<DnsRecord>> GetRecordsForZoneAsync(string zoneId);
    Task CreateRecord(IPAddress address, string zoneId, string recordName, bool enableProxy);
    Task UpdateRecord(UpdateDnsRecordRequest update);
    Task DeleteRecord(string zoneId, string recordId);
}

public class CloudflareApiClient : ICloudflareApiClient
{
    public const string ClientName = "Cloudflare";

    private readonly HttpClient _client;
    
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public CloudflareApiClient(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient(ClientName);
    }

    public async Task<IEnumerable<DnsRecord>> GetRecordsForZoneAsync(string zoneId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/client/v4/zones/{zoneId}/dns_records");
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResult<DnsRecord>>(_jsonSerializerOptions);
        if (result is null)
            throw new Exception("Cloudflare API returned null");

        return result.Result;
    }

    public async Task CreateRecord(IPAddress address, string zoneId, string recordName, bool enableProxy)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/client/v4/zones/{zoneId}/dns_records");
        request.Content = JsonContent.Create(new
        {
            content = address.ToString(),
            name = recordName,
            type = address.AddressFamily == AddressFamily.InterNetwork ? "A" : "AAAA",
            comment = "DynamicIpUpdater",
            proxied = enableProxy,
            ttl = 1
        }, options: _jsonSerializerOptions);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRecord(UpdateDnsRecordRequest update)
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"/client/v4/zones/{update.ZoneId}/dns_records/{update.RecordId}");
        request.Content = JsonContent.Create(new
        {
            content = update.Content,
            proxied = update.Proxied,
            type = update.Type,
            name = update.Name,
            comment = "Automatically updated by Stipps.CloudflareIpUpdater"
        }, options: _jsonSerializerOptions);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRecord(string zoneId, string recordId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/client/v4/zones/{zoneId}/dns_records/{recordId}");
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}