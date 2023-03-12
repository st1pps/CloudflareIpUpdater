using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stipps.CloudflareApi.Configuration;

namespace Stipps.CloudflareApi.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudflareApi(this IServiceCollection services)
    {
        services.AddHttpClient(CloudflareApiClient.ClientName, (svc,client) =>
        {
            var settings = svc.GetRequiredService<IOptions<CloudflareConnectionSettings>>();
            client.BaseAddress = new Uri("https://api.cloudflare.com");
            client.DefaultRequestHeaders.Add("X-Auth-Email", settings.Value.Email);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.Value.ApiToken);
        });
        services.AddSingleton<ICloudflareApiClient, CloudflareApiClient>();
        return services;
    }
}