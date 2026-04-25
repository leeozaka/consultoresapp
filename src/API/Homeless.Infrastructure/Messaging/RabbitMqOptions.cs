namespace Homeless.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "consultores";
    public string Password { get; set; } = "consultores";
    public ushort PrefetchCount { get; set; } = 16;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
