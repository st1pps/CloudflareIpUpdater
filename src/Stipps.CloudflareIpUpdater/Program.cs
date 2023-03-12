using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;
using Stipps.CloudflareApi.Configuration;
using Stipps.CloudflareApi.Extensions.DependencyInjection;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Jobs;
using Stipps.CloudflareIpUpdater.Services;

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
        .ConfigureAppConfiguration(config =>
        {
            config.AddCommandLine(args);
            config.AddJsonFile("appsettings.json", optional: true);
            config.AddEnvironmentVariables();
            config.AddUserSecrets<Program>();
        })
        .ConfigureServices((context, services) =>
        {
            services.AddLogging();
            
            services.AddOptions<CloudflareConnectionSettings>()
                .Configure(settings=> context.Configuration.GetSection(CloudflareConnectionSettings.SectionName).Bind(settings))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            services.AddOptions<CloudflareServiceSettings>()
                .Configure(settings =>
                    context.Configuration.GetSection(CloudflareServiceSettings.SectionName).Bind(settings))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            services.AddHttpClient();
            services.AddCloudflareApi();
            services.AddScoped<UpdateDnsJob>();
            services.AddScoped<CloudflareService>();
            services.AddScoped<IpService>();
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.UseSimpleTypeLoader();
                q.UseInMemoryStore();

                var interval = context.Configuration.GetValue<int?>("UpdateIntervalMinutes") ?? 5;
                q.ScheduleJob<UpdateDnsJob>(trigger =>
                    trigger.WithIdentity("Update DNS trigger")
                        .WithSimpleSchedule(schedule => schedule.WithIntervalInMinutes(interval).RepeatForever())
                        .WithDescription("Update DNS")
                        .StartNow());
            });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        })
        .UseSerilog()
        .Build();