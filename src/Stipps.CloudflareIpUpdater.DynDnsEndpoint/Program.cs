using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Stipps.CloudflareIpUpdater.Extensions.DependencyInjection;
using Stipps.CloudflareIpUpdater.Models;
using Stipps.CloudflareIpUpdater.Services;
using Stipps.CloudflareIpUpdates.DynDnsEndpoint;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application...");
    var app = BuildApplication(args);
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Web application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static WebApplication BuildApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "appsettings.json"),
            optional: false);

    Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    
    builder.Services
        .AddCloudflareService(builder.Configuration)
        .AddOptions<DynDnsCredentials>()
        .Configure(cred => builder.Configuration.GetSection(DynDnsCredentials.SectionName).Bind(cred))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    
    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.MapGet("/update",
        async (string username, string password, string? ipv4, string? ipv6, IOptions<DynDnsCredentials> credentials, CloudflareService service) =>
        {
            if (username != credentials.Value.Username || password != credentials.Value.Password)
            {
                return Results.Forbid();
            }
    
            var v4Success = IPAddress.TryParse(ipv4, out var v4);
            var v6Success = IPAddress.TryParse(ipv6, out var v6);
            
            if(!v4Success && !v6Success)
            {
                return Results.BadRequest();
            }
    
            try
            {
                await service.UpdateIp(new IpValues(v4, v6));
            }
            catch
            {
                return Results.StatusCode(500);
            }
    
            return Results.Ok();
    
        });

    return app;
}