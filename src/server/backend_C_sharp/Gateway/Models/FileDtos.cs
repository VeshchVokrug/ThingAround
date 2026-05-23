namespace Gateway.Models;

public record UploadUrlRequest
{
    public string FileName { get; init; } = string.Empty;
    public string? Folder { get; init; }
}

public record UploadUrlResponse
{
    public string UploadUrl { get; init; } = string.Empty;
    public string PublicUrl { get; init; } = string.Empty;
}