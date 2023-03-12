using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Stipps.CloudflareApi;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Requests;
using Stipps.CloudflareIpUpdater.Configuration;
using Stipps.CloudflareIpUpdater.Services;

namespace Stipps.CloudflareIpUpdaterUnitTests;

public class CloudflareServiceTests
{
    private readonly CloudflareService _sut;
    private readonly ICloudflareApiClient _apiClient = Substitute.For<ICloudflareApiClient>();
    private readonly ILogger<CloudflareService> _logger = new NullLogger<CloudflareService>();
    private readonly IPAddress[] _ips = { IPAddress.Parse("1.2.3.4"), IPAddress.Parse("2001:db8::1") };
    
    private const string ZoneName = "ZoneName.com";

    private static readonly string[] Ipv4Ids = {
        "IPV4-NO1-FA36-4FE2-ABD9-02795DAA1E94",
        "IPV4-NO2-FA36-4FE2-ABD9-02795DAA1E95",
        "IPV4-NO3-FA36-4FE2-ABD9-02795DAA1E96",
        "IPV4-NO4-FA36-4FE2-ABD9-02795DAA1E97",
    };
    
    private static readonly string[] Ipv6Ids = {
        "IPV6-NO1-947B-42DE-B44B-1458328619D6",
        "IPV6-NO2-947B-42DE-B44B-1458328619D7",
        "IPV6-NO3-947B-42DE-B44B-1458328619D8",
        "IPV6-NO4-947B-42DE-B44B-1458328619D9",
    };

    private readonly IOptions<CloudflareServiceSettings> _settings = Options.Create(new CloudflareServiceSettings
    {
        ZoneId = "ZoneId",
        RecordName = "RecordName",
        ProxyEnabled = true
    });

    public CloudflareServiceTests()
    {
        _sut = new CloudflareService(_logger, _apiClient, _settings);
    }

    [Fact]
    public async Task IPv4AndIPv6_AreCorrect()
    {
        // Arrange
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(_ips.Select(ip => new DnsRecord
            {
                Type = ip.AddressFamily == AddressFamily.InterNetwork ? DnsRecordType.A: DnsRecordType.AAAA,
                Content = ip.ToString(),
                CreatedOn = DateTimeOffset.Now,
                ModifiedOn = DateTimeOffset.Now,
                Id = ip.AddressFamily == AddressFamily.InterNetwork ? Ipv4Ids[0] : Ipv6Ids[0],
                Name = $"{_settings.Value.RecordName}.{ZoneName}",
                Proxied = _settings.Value.ProxyEnabled,
                ZoneId = _settings.Value.ZoneId,
                ZoneName = ZoneName,
                Ttl = 1
            }));

        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);

        // Assert

        await _apiClient.DidNotReceive().UpdateRecord(Arg.Any<UpdateDnsRecordRequest>());
        await _apiClient.DidNotReceive().DeleteRecord(Arg.Any<string>(), Arg.Any<string>());
        await _apiClient.DidNotReceive()
            .CreateRecord(Arg.Any<CreateDnsRecordRequest>());
    }

    [Fact]
    public async Task NoRecordsExist()
    {
        // Arrange 
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(Enumerable.Empty<DnsRecord>());
        
        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);
        
        // Assert
        await _apiClient.ReceivedWithAnyArgs(2).CreateRecord(default!);
        await _apiClient.Received().CreateRecord(Arg.Is<CreateDnsRecordRequest>(arg =>
            arg.Content == _ips[0].ToString() && arg.ZoneId == _settings.Value.ZoneId &&
            arg.Name == _settings.Value.RecordName && arg.Proxied == _settings.Value.ProxyEnabled));
        await _apiClient.Received().CreateRecord(Arg.Is<CreateDnsRecordRequest>(arg =>
            arg.Content == _ips[1].ToString() && arg.ZoneId == _settings.Value.ZoneId &&
            arg.Name == _settings.Value.RecordName && arg.Proxied == _settings.Value.ProxyEnabled));
        await _apiClient.DidNotReceiveWithAnyArgs().DeleteRecord(default!, default!);
        await _apiClient.DidNotReceiveWithAnyArgs().UpdateRecord(default!);
    }

    [Fact]
    public async Task IPv4Correct_IPv6Missing()
    {
        // Arrange
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(new[]
            {
                new DnsRecord
                {
                    Type = DnsRecordType.A,
                    Content = _ips[0].ToString(),
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id =Ipv4Ids[0],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                }
            });
        
        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);
        
        // Assert
        await _apiClient.Received().CreateRecord(Arg.Is<CreateDnsRecordRequest>(arg => arg.Content == _ips[1].ToString() && arg.ZoneId == _settings.Value.ZoneId && arg.Name == _settings.Value.RecordName && arg.Proxied == _settings.Value.ProxyEnabled));
        await _apiClient.DidNotReceiveWithAnyArgs().DeleteRecord(default!, default!);
        await _apiClient.DidNotReceiveWithAnyArgs().UpdateRecord(default!);
    }

    [Fact]
    public async Task IPv4False_IPv6Missing()
    {
        // Arrange
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(new[]
            {
                new DnsRecord
                {
                    Type = DnsRecordType.A,
                    Content = "4.3.2.1",
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id = Ipv4Ids[0],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                }
            });
        
        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);
        
        // Assert
        await _apiClient.Received().UpdateRecord(Arg.Is<UpdateDnsRecordRequest>(ip => ip.Content == _ips[0].ToString()));
        await _apiClient.Received().CreateRecord(Arg.Is<CreateDnsRecordRequest>(arg =>
            arg.Content == _ips[1].ToString() && arg.ZoneId == _settings.Value.ZoneId &&
            arg.Name == _settings.Value.RecordName && arg.Proxied == _settings.Value.ProxyEnabled));
        await _apiClient.DidNotReceiveWithAnyArgs().DeleteRecord(default!, default!);
    }
    
    [Fact]
    public async Task IPv4Missing_IPv6Correct()
    {
        // Arrange
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(new[]
            {
                new DnsRecord
                {
                    Type = DnsRecordType.AAAA,
                    Content = _ips[1].ToString(),
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id = Ipv6Ids[0],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                }
            });
        
        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);
        
        // Assert
        await _apiClient.Received().CreateRecord(Arg.Is<CreateDnsRecordRequest>(arg => arg.Content == _ips[0].ToString() && arg.ZoneId == _settings.Value.ZoneId && arg.Name == _settings.Value.RecordName && arg.Proxied == _settings.Value.ProxyEnabled));
        await _apiClient.DidNotReceiveWithAnyArgs().DeleteRecord(default!, default!);
        await _apiClient.DidNotReceiveWithAnyArgs().UpdateRecord(default!);
    }

    [Fact]
    public async Task TwoIncorrectIPv4_CorrectIPv6()
    {
        // Arrange
        _apiClient.GetRecordsForZoneAsync(Arg.Any<string>())
            .Returns(new[]
            {
                new DnsRecord
                {
                    Type = DnsRecordType.A,
                    Content = "6.6.6.6",
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id = Ipv4Ids[0],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                },
                new DnsRecord
                {
                    Type = DnsRecordType.A,
                    Content = "7.7.7.7",
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id = Ipv4Ids[1],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                },
                new DnsRecord
                {
                    Type = DnsRecordType.AAAA,
                    Content = _ips[1].ToString(),
                    CreatedOn = DateTimeOffset.Now,
                    ModifiedOn = DateTimeOffset.Now,
                    Id = Ipv6Ids[0],
                    Name = $"{_settings.Value.RecordName}.{ZoneName}",
                    Proxied = _settings.Value.ProxyEnabled,
                    ZoneId = _settings.Value.ZoneId,
                    ZoneName = ZoneName,
                    Ttl = 1
                }
            });

        // Act
        await _sut.UpdateIp(_ips[0], _ips[1]);

        // Assert
        await _apiClient.DidNotReceive().CreateRecord(Arg.Any<CreateDnsRecordRequest>());
        await _apiClient.Received(1).DeleteRecord(_settings.Value.ZoneId, Ipv4Ids[1]);
        await _apiClient.Received(1)
            .UpdateRecord(Arg.Is<UpdateDnsRecordRequest>(e =>
                e.RecordId == Ipv4Ids[0] && e.ZoneId == _settings.Value.ZoneId && e.Content == _ips[0].ToString()));
    }
}