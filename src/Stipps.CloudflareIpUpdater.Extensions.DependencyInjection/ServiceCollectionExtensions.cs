using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stipps.CloudflareApi.Extensions.DependencyInjection;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Services;

namespace Stipps.CloudflareIpUpdater.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudflareService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddCloudflareApi(configuration);
        
        services.AddMemoryCache();
            
        services.AddOptions<CloudflareServiceSettings>()
            .Configure(settings =>
                configuration.GetSection(CloudflareServiceSettings.SectionName).Bind(settings))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<CloudflareService>();
        return services;
    }
}