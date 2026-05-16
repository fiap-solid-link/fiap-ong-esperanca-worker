using Esperanca.Worker.Service.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Esperanca.Worker.Service.RabbitMq;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task<IConnection> CreateConnectionAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        return await factory.CreateConnectionAsync(ct);
    }
}
