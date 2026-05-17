using Esperanca.Worker.Service.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Esperanca.Worker.Service.Mongo;

public sealed class DoacaoMongoService
{
    private readonly IMongoCollection<DoacaoDocument> _collection;

    public DoacaoMongoService(IMongoClient mongoClient, IOptions<MongoOptions> options)
    {
        var mongoOptions = options.Value;
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        _collection = database.GetCollection<DoacaoDocument>(mongoOptions.DoacoesCollection);
    }

    public async Task CriarIndiceAsync(CancellationToken ct)
    {
        var keys = Builders<DoacaoDocument>.IndexKeys.Ascending(x => x.IdempotencyKey);
        var index = new CreateIndexModel<DoacaoDocument>(
            keys,
            new CreateIndexOptions { Unique = true, Name = "ux_doacoes_idempotency_key" });

        await _collection.Indexes.CreateOneAsync(index, cancellationToken: ct);
    }

    public async Task<bool> ExistePorIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct)
    {
        return await _collection
            .Find(x => x.IdempotencyKey == idempotencyKey.ToString())
            .AnyAsync(ct);
    }

    public async Task InserirAsync(DoacaoDocument document, CancellationToken ct)
    {
        await _collection.InsertOneAsync(document, cancellationToken: ct);
    }
}
