using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stipps.CloudflareApi.Configuration;

namespace Stipps.CloudflareApi.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudflareApi(this IServiceCollection services, IConfiguration config)
    {
        var settings = new CloudflareConnectionSettings();
        config.GetSection(CloudflareConnectionSettings.SectionName).Bind(settings);
        CloudflareConnectionSettings.Validate(settings);
        services.AddSingleton(settings);
        
        services.AddHttpClient(CloudflareApiClient.ClientName, (svc,client) =>
        {
            var s = svc.GetRequiredService<CloudflareConnectionSettings>();
            client.BaseAddress = new Uri("https://api.cloudflare.com");
            client.DefaultRequestHeaders.Add("X-Auth-Email", s.Email);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", s.ApiToken);
        });
        services.AddSingleton<ICloudflareApiClient, CloudflareApiClient>();
        return services;
    }
}