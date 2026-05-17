using RabbitMQ.Client;

namespace Esperanca.Worker.Service.RabbitMq;

internal static class RabbitMqTopology
{
    public static async Task DeclareDeadLetterAsync(
        IChannel channel,
        string deadLetterExchange,
        string deadLetterQueue,
        CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: deadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: deadLetterQueue,
            exchange: deadLetterExchange,
            routingKey: string.Empty,
            cancellationToken: ct);
    }

    public static async Task DeclareWorkQueueAsync(
        IChannel channel,
        string exchange,
        string queue,
        string routingKey,
        string deadLetterExchange,
        CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchange
        };

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: routingKey,
            cancellationToken: ct);
    }
}
