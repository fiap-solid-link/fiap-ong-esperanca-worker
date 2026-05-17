namespace Esperanca.Worker.Service.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";

    public string Exchange { get; init; } = "esperanca.doacoes";
    public string DeadLetterExchange { get; init; } = "esperanca.doacoes.dlx";

    public string RecebidaQueue { get; init; } = "doacoes-recebidas";
    public string RecebidaRoutingKey { get; init; } = "recebida";
    public string RecebidaDeadLetterQueue { get; init; } = "doacoes-recebidas-dlq";

    public string ProcessadaQueue { get; init; } = "doacoes-processadas";
    public string ProcessadaRoutingKey { get; init; } = "processada";
    public string ProcessadaDeadLetterQueue { get; init; } = "doacoes-processadas-dlq";

    public ushort PrefetchCount { get; init; } = 1;
}
