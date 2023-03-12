namespace Stipps.CloudflareApi;

public class ApiMessage
{
    public int Code { get; set; }
    public required string Message { get; init; }
}