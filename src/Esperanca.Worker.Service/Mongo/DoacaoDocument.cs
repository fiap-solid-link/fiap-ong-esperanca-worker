using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Esperanca.Worker.Service.Mongo;

public sealed class DoacaoDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string IdDoacao { get; set; } = string.Empty;

    public string IdCampanha { get; set; } = string.Empty;

    public string IdDoador { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime DataIntencao { get; set; }

    public DateTime DataProcessamento { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
}
