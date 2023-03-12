using Microsoft.Extensions.Logging;
using Quartz;
using Stipps.CloudflareIpUpdater.Services;

namespace Stipps.CloudflareIpUpdater.Jobs;

public class UpdateDnsJob : IJob
{
    private readonly ILogger<UpdateDnsJob> _logger; 
    private readonly IpService _ipService;
    private readonly CloudflareService _cloudflareService;

    public UpdateDnsJob(CloudflareService cloudflareService, IpService ipService, ILogger<UpdateDnsJob> logger)
    {
        _cloudflareService = cloudflareService;
        _ipService = ipService;
        _logger = logger;
    }

    private async Task UpdateDns()
    {
        _logger.LogInformation("Starting DNS update...");
        var (v4, v6) = await _ipService.GetIpAsync();
        await _cloudflareService.UpdateIp(v4, v6);
        _logger.LogInformation("Finished DNS update");
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await UpdateDns();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DNS: {Message}", ex.Message);
        }
    }
}