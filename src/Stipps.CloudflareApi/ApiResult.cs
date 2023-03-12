namespace Stipps.CloudflareApi;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public ApiMessage[] Errors { get; set; } = Array.Empty<ApiMessage>();
    public ApiMessage[] Messages { get; set; } = Array.Empty<ApiMessage>();
    public ApiResultInfo ResultInfo { get; set; } = new(); 
    public T[] Result { get; set; } = Array.Empty<T>();
}


public class ApiResultInfo
{
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Count { get; set; }
    public int TotalCount { get; set; }
}