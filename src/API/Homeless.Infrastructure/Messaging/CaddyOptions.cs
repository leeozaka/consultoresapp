namespace Homeless.Infrastructure.Messaging;

public sealed class CaddyOptions
{
    public const string SectionName = "Caddy";

    public string AdminUrl { get; set; } = "http://caddy:2019";
    public string HttpServerName { get; set; } = "srv0";
    public string ApiUpstream { get; set; } = "api:8080";
    public string DefaultFrontendUpstream { get; set; } = "frontend:4000";
}
