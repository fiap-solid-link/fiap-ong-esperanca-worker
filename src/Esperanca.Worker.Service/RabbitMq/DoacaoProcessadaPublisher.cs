using Esperanca.Message.Events;
using Esperanca.Worker.Service.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace Esperanca.Worker.Service.RabbitMq;

public sealed class DoacaoProcessadaPublisher(
    IOptions<RabbitMqOptions> options,
    RabbitMqConnectionFactory connectionFactory,
    ILogger<DoacaoProcessadaPublisher> logger) : IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublicarAsync(DoacaoProcessadaEvent evento, CancellationToken ct)
    {
        var channel = await EnsureChannelAsync(ct);
        var body = JsonSerializer.SerializeToUtf8Bytes(evento);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = evento.IdDoacao.ToString(),
            CorrelationId = evento.IdDoacao.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: _options.ProcessadaRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        logger.LogInformation(
            "DoacaoProcessada publicada em {Exchange}/{RoutingKey}. IdDoacao={IdDoacao}, IdCampanha={IdCampanha}",
            _options.Exchange,
            _options.ProcessadaRoutingKey,
            evento.IdDoacao,
            evento.IdCampanha);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            _connection ??= await connectionFactory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await RabbitMqTopology.DeclareDeadLetterAsync(
                _channel,
                _options.DeadLetterExchange,
                _options.ProcessadaDeadLetterQueue,
                ct);

            await RabbitMqTopology.DeclareWorkQueueAsync(
                _channel,
                _options.Exchange,
                _options.ProcessadaQueue,
                _options.ProcessadaRoutingKey,
                _options.DeadLetterExchange,
                ct);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        _gate.Dispose();
    }
}
