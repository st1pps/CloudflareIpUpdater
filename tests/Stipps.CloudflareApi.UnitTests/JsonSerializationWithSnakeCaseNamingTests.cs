using System.Text.Json;
using System.Text.Json.Serialization;
using Stipps.CloudflareApi.Models;
using Stipps.CloudflareApi.Serialization;

namespace Stipps.CloudflareApi.UnitTests;

public class JsonSerializationWithSnakeCaseNamingTests
{
    private readonly JsonSerializerOptions _serializerOptions;

    public JsonSerializationWithSnakeCaseNamingTests()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public void CanSerializeObjectToJson()
    {
        // Arrange
        var dnsRecord = new DnsRecord
        {
            Content = "1.2.3.4",
            Type = DnsRecordType.A,
            Name = "test.example.com",
            ZoneName = "example.com",
            ZoneId = "1234567890",
            Id = "0987654321",
            CreatedOn = DateTimeOffset.Now,
            ModifiedOn = DateTimeOffset.Now,
            Meta = new DnsRecordMeta
            {
                AutoAdded = true
            }
        };
        
        // Act
        var json = JsonSerializer.Serialize(dnsRecord, _serializerOptions);
        
        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().ContainAll("zone_name", "zone_id", "auto_added", "created_on", "modified_on", "\"A\"");
    }

    [Fact]
    public void CanDeserializeJsonToObject()
    {
        // Arrange
        const string json = """
            {
                "id": "0987654321",
                "zone_id": "1234567890",
                "zone_name": "example.com",
                "name": "test.example.com",
                "type": "A",
                "content": "1.2.3.4",
                "created_on": "2021-08-01T12:00:00Z",
                "modified_on": "2021-08-01T12:00:00Z",
                "meta": {
                    "auto_added": true
                }
            }
            """;

        // Act
        var dnsRecord = JsonSerializer.Deserialize<DnsRecord>(json, _serializerOptions);

        // Assert
        dnsRecord.Should().NotBeNull();
        dnsRecord!.Id.Should().Be("0987654321");
        dnsRecord.Type.Should().Be(DnsRecordType.A);
        dnsRecord.Meta.Should().NotBeNull();
        dnsRecord.Meta!.AutoAdded.Should().BeTrue();
    }
}