using Esperanca.Worker.Service.Mongo;
using Esperanca.Worker.Service.Options;
using Esperanca.Worker.Service.RabbitMq;
using Esperanca.Worker.Service.Workers;
using MongoDB.Driver;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.Configure<MongoOptions>(
    builder.Configuration.GetSection(MongoOptions.SectionName));

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDb");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:MongoDb não configurada.");
    }

    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<DoacaoMongoService>();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<DoacaoProcessadaPublisher>();

builder.Services.AddHostedService<DoacaoWorker>();

builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
});

var host = builder.Build();

await host.RunAsync();
