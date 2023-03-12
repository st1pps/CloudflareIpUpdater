using System.Text.Json;
using System.Text.Json.Serialization;
using Stipps.CloudflareApi.Models;

namespace Stipps.CloudflareApi.Converters;

public class DnsRecordTypeConverter : JsonConverter<DnsRecordType>
{
    public override DnsRecordType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "A" => DnsRecordType.A,
            "AAAA" => DnsRecordType.AAAA,
            _ => DnsRecordType.Unsupported
        };
    }

    public override void Write(Utf8JsonWriter writer, DnsRecordType value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            DnsRecordType.A => "A",
            DnsRecordType.AAAA => "AAAA",
            _ => throw new JsonException($"Unsupported DNS record type: {value}")
        };
        writer.WriteStringValue(str);
    }
}