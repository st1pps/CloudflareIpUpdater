using System.Text.Json;

namespace Stipps.CloudflareApi.Serialization;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return name.ToSnakeCase();
    }
}