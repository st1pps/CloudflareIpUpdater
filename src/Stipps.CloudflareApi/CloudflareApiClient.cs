using System.Net.Http.Json;
using System.Text.Json;
using Stipps.CloudflareApi.Converters;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Requests;
using Stipps.CloudflareApi.Serialization;

namespace Stipps.CloudflareApi;

public interface ICloudflareApiClient
{
    Task<IEnumerable<DnsRecord>> GetRecordsForZoneAsync(string zoneId);
    Task CreateRecord(CreateDnsRecordRequest create);
    Task UpdateRecord(UpdateDnsRecordRequest update);
    Task DeleteRecord(string zoneId, string recordId);
}

public class CloudflareApiClient : ICloudflareApiClient
{
    public const string ClientName = "Cloudflare";

    private readonly HttpClient _client;
    
    public static JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        Converters = { new DnsRecordTypeConverter() },
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
        var result = await response.Content.ReadFromJsonAsync<ApiResult<DnsRecord>>(JsonSerializerOptions);
        if (result is null)
            throw new Exception("Cloudflare API returned null");

        return result.Result;
    }

    public async Task CreateRecord(CreateDnsRecordRequest create)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/client/v4/zones/{create.ZoneId}/dns_records");
        
        request.Content = JsonContent.Create(create, options: JsonSerializerOptions);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRecord(UpdateDnsRecordRequest update)
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"/client/v4/zones/{update.ZoneId}/dns_records/{update.RecordId}");
        request.Content = JsonContent.Create(update, options: JsonSerializerOptions);
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