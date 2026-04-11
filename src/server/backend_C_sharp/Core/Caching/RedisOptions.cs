namespace Core.Caching;

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? InstancePrefix { get; set; }
}