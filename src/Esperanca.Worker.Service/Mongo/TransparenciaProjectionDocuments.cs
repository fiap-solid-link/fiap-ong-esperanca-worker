using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Esperanca.Worker.Service.Mongo;

[BsonIgnoreExtraElements]
internal sealed class PainelMacroProjectionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("totalArrecadado")]
    public decimal TotalArrecadado { get; set; }

    [BsonElement("totalDoacoes")]
    public int TotalDoacoes { get; set; }

    [BsonElement("atualizadoEm")]
    public DateTime AtualizadoEm { get; set; }
}

[BsonIgnoreExtraElements]
internal sealed class CampanhaListaProjectionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("idCampanha")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid IdCampanha { get; set; }

    [BsonElement("valorArrecadado")]
    public decimal ValorArrecadado { get; set; }
}

[BsonIgnoreExtraElements]
internal sealed class CampanhaDetalheProjectionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("idCampanha")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid IdCampanha { get; set; }

    [BsonElement("valorArrecadado")]
    public decimal ValorArrecadado { get; set; }

    [BsonElement("doacoes")]
    public List<DoacaoAnonimaProjectionDocument> Doacoes { get; set; } = [];
}

[BsonIgnoreExtraElements]
internal sealed class DoacaoAnonimaProjectionDocument
{
    [BsonElement("apelidoDoador")]
    public string ApelidoDoador { get; init; } = string.Empty;

    [BsonElement("valor")]
    public decimal Valor { get; init; }

    [BsonElement("data")]
    public DateTime Data { get; init; }
}
