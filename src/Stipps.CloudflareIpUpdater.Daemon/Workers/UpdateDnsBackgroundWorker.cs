using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Services;

namespace Stipps.CloudflareIpUpdater.Workers;

public class UpdateDnsBackgroundWorker : BackgroundService
{
    private readonly ILogger<UpdateDnsBackgroundWorker> _logger;
    private readonly CloudflareServiceSettings _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UpdateDnsBackgroundWorker(ILogger<UpdateDnsBackgroundWorker> logger, IOptions<CloudflareServiceSettings> settings, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryUpdateDns(stoppingToken);
        
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.UpdateIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryUpdateDns(stoppingToken);
        }
    }

    private async Task TryUpdateDns(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting DNS update...");
            await UpdateDns(ct);
            _logger.LogInformation("Finished DNS update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DNS: {Message}", ex.Message);
        }
    }
    
    private async Task UpdateDns(CancellationToken ct)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        
        var ipService = scope.ServiceProvider.GetRequiredService<IpService>();
        var values = await ipService.GetIpAsync(ct);
        
        var cloudflareService = scope.ServiceProvider.GetRequiredService<CloudflareService>();
        await cloudflareService.UpdateIp(values);
    }
}