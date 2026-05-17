using Esperanca.Message.Events;
using Esperanca.Worker.Service.Mongo;
using Esperanca.Worker.Service.Options;
using Esperanca.Worker.Service.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Esperanca.Worker.Service.Workers;

public sealed class DoacaoWorker(
    IOptions<RabbitMqOptions> options,
    RabbitMqConnectionFactory connectionFactory,
    DoacaoMongoService doacaoMongoService,
    DoacaoProcessadaPublisher doacaoProcessadaPublisher,
    ILogger<DoacaoWorker> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await doacaoMongoService.CriarIndiceAsync(stoppingToken);
            await ConnectAndConsumeAsync(stoppingToken);
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Worker de doações encerrado.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Worker de doações encerrou inesperadamente.", ex);
        }
    }

    private async Task ConnectAndConsumeAsync(CancellationToken ct)
    {
        logger.LogInformation("Conectando ao RabbitMQ em {Host}:{Port}", _options.Host, _options.Port);

        _connection = await connectionFactory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await RabbitMqTopology.DeclareDeadLetterAsync(
            _channel,
            _options.DeadLetterExchange,
            _options.RecebidaDeadLetterQueue,
            ct);

        await RabbitMqTopology.DeclareWorkQueueAsync(
            _channel,
            _options.Exchange,
            _options.RecebidaQueue,
            _options.RecebidaRoutingKey,
            _options.DeadLetterExchange,
            ct);

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

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.RecebidaQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        logger.LogInformation(
            "Worker ouvindo {Queue} com routing key {RoutingKey}. Prefetch={PrefetchCount}",
            _options.RecebidaQueue,
            _options.RecebidaRoutingKey,
            _options.PrefetchCount);

        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
            return;

        var deliveryTag = ea.DeliveryTag;

        try
        {
            var evento = JsonSerializer.Deserialize<DoacaoRecebida>(ea.Body.Span)
                         ?? throw new InvalidOperationException("Payload DoacaoRecebida vazio.");

            logger.LogInformation(
                "DoacaoRecebida recebida. IdDoacao={IdDoacao}, IdCampanha={IdCampanha}, Valor={Valor}",
                evento.IdDoacao,
                evento.IdCampanha,
                evento.Valor);

            var jaProcessada = await doacaoMongoService.ExistePorIdempotencyKeyAsync(
                evento.IdempotencyKey,
                CancellationToken.None);

            if (jaProcessada)
            {
                logger.LogInformation(
                    "Doação já processada pelo worker. IdDoacao={IdDoacao}, IdempotencyKey={IdempotencyKey}",
                    evento.IdDoacao,
                    evento.IdempotencyKey);

                await _channel.BasicAckAsync(deliveryTag, multiple: false);
                return;
            }

            var dataProcessamento = DateTime.UtcNow;

            var document = new DoacaoDocument
            {
                IdDoacao = evento.IdDoacao.ToString(),
                IdCampanha = evento.IdCampanha.ToString(),
                IdDoador = evento.IdDoador.ToString(),
                Valor = evento.Valor,
                DataIntencao = evento.DataIntencao,
                DataProcessamento = dataProcessamento,
                IdempotencyKey = evento.IdempotencyKey.ToString()
            };

            await doacaoMongoService.InserirAsync(document, CancellationToken.None);

            var processada = new DoacaoProcessadaEvent(
                evento.IdDoacao,
                evento.IdCampanha,
                evento.Valor,
                dataProcessamento);

            await doacaoProcessadaPublisher.PublicarAsync(processada, CancellationToken.None);

            await _channel.BasicAckAsync(deliveryTag, multiple: false);

            logger.LogInformation(
                "Doação processada com sucesso. IdDoacao={IdDoacao}, IdCampanha={IdCampanha}",
                evento.IdDoacao,
                evento.IdCampanha);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Payload inválido recebido. Enviando mensagem para DLQ. DeliveryTag={DeliveryTag}", deliveryTag);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar doação. Enviando mensagem para DLQ. DeliveryTag={DeliveryTag}", deliveryTag);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
