using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Level}] ({SourceContext}) {Message}{NewLine}{Exception}")
    .CreateBootstrapLogger();

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
    builder.Services.AddSerilog((services, lc) =>
    {
        lc
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Http", LogEventLevel.Warning)
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            
            .WriteTo.Console(outputTemplate: "[{Level}] ({SourceContext}) {Message}{NewLine}{Exception}");
    });
    
    builder.Services
        .AddCloudflareService(builder.Configuration)
        .AddOptions<DynDnsCredentials>()
        .Configure(cred => builder.Configuration.GetSection(DynDnsCredentials.SectionName).Bind(cred))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    
    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.MapGet("/update",
        async ([FromServices]ILoggerFactory loggerFactory, string username, string password, string? ipv4, string? ipv6, IOptions<DynDnsCredentials> credentials, CloudflareService service) =>
        {
            var logger = loggerFactory.CreateLogger("UpdateEndpoint");
            if (username != credentials.Value.Username || password != credentials.Value.Password)
            {
                logger.LogInformation("Unauthorized request to update IP address.");
                return Results.StatusCode(403);
            }
    
            var v4Success = IPAddress.TryParse(ipv4, out var v4);
            var v6Success = IPAddress.TryParse(ipv6, out var v6);
            
            if(!v4Success && !v6Success)
            {
                logger.LogWarning("Invalid IP address format.");
                return Results.BadRequest();
            }
    
            try
            {
                logger.LogInformation("Updating IP address to {v4} and {v6}", v4, v6);
                await service.UpdateIp(new IpValues(v4, v6));
            }
            catch
            {
                logger.LogError("Failed to update IP address.");
                return Results.StatusCode(500);
            }
    
            return Results.Ok();
    
        });
    
    return app;
}