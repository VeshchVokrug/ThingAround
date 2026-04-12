namespace Gateway.Models.Configuration;

public sealed record GrpcEndpointsOptions
{
    public const string SectionName = "GrpcEndpoints";
    public string IdentityProfileService { get; init; } = string.Empty;
}