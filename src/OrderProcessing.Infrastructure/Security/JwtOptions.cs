namespace OrderProcessing.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OrderProcessing";
    public string Audience { get; init; } = "OrderProcessing.Client";
    public string SigningKey { get; init; } = "development-only-signing-key-change-me-please";
}
