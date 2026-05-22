namespace Gateway.Models.Configuration;

public sealed record GrpcEndpointsOptions
{
    public const string SectionName = "GrpcEndpoints";
    public string IdentityProfileService { get; init; } = string.Empty;
    public string CatalogService { get; init; } = string.Empty;
    public string RentalService { get; init; } = string.Empty;
}