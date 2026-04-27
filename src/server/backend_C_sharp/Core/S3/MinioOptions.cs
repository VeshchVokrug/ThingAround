namespace Core.S3;

public class MinioOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ExternalEndpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "default";
    public bool UseHttp { get; set; } = true;
    
    public List<string> AllowedOrigins { get; set; } = ["*"];
}