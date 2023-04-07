using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Stipps.CloudflareIpUpdater.Exceptions;
using Stipps.CloudflareIpUpdater.Models;

namespace Stipps.CloudflareIpUpdater.Services;

public class IpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IpService> _logger;

    private const string IpV4Endpoint = "https://api.ipify.org";
    private const string IpV6Endpoint = "https://api64.ipify.org";

    public IpService(IHttpClientFactory httpClientFactory, ILogger<IpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IpValues> GetIpAsync(CancellationToken ct)
    {
        _logger.LogInformation("Getting IP address from ipify.org...");
        using var client = _httpClientFactory.CreateClient("Ipify");
        
        ct.ThrowIfCancellationRequested();
        var v4 = await GetIPv4Address(client);
        ct.ThrowIfCancellationRequested();
        var v6 = await GetIPv6Address(client);
        if (v4 is null && v6 is null)
        {
            throw new ProcureIpException("Unable to determine IP address");
        }

        return new IpValues(v4, v6);
    }

    private async Task<IPAddress?> GetIPv4Address(HttpClient client)
    {
        var response = await client.GetAsync(IpV4Endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Got IP address from ipify.org: {Ip}", content);
        return IPAddress.TryParse(content, out var v4) && v4.AddressFamily == AddressFamily.InterNetwork ? v4 : null;
    }

    private async Task<IPAddress?> GetIPv6Address(HttpClient client)
    {
        var response = await client.GetAsync(IpV6Endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Got IP address from ipify.org: {Ip}", content);
        return IPAddress.TryParse(content, out var v6) && v6.AddressFamily == AddressFamily.InterNetworkV6 ? v6 : null;
    }
}