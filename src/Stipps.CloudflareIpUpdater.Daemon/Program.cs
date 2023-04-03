using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Stipps.CloudflareApi.Extensions.DependencyInjection;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Services;
using Stipps.CloudflareIpUpdater.Workers;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting host...");
    BuildHost(args).Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}


static IHost BuildHost(string[] args) =>
    new HostBuilder()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(context.HostingEnvironment.ContentRootPath);
            config.AddCommandLine(args);
            config.AddJsonFile(Path.Combine("Settings","appsettings.json"), optional: false);
            config.AddEnvironmentVariables();
            config.AddUserSecrets<Program>();
        })
        .ConfigureServices((context, services) =>
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration).CreateLogger();
            services.AddLogging();
            services.AddMemoryCache();
            
            services.AddOptions<CloudflareServiceSettings>()
                .Configure(settings =>
                    context.Configuration.GetSection(CloudflareServiceSettings.SectionName).Bind(settings))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            services.AddHttpClient();
            services.AddCloudflareApi(context.Configuration);
            services.AddSingleton<UpdateDnsBackgroundWorker>();
            services.AddHostedService(provider => provider.GetRequiredService<UpdateDnsBackgroundWorker>());
            services.AddScoped<CloudflareService>();
            services.AddScoped<IpService>();
        })
        .UseSerilog()
        .Build();