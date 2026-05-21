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

    [BsonElement("totalCampanhasAtivas")]
    public int TotalCampanhasAtivas { get; set; }

    [BsonElement("totalCampanhasConcluidas")]
    public int TotalCampanhasConcluidas { get; set; }

    [BsonElement("topDoadores")]
    public List<TopDoadorProjectionDocument> TopDoadores { get; set; } = [];

    [BsonElement("atualizadoEm")]
    public DateTime AtualizadoEm { get; set; }
}

internal sealed class TopDoadorProjectionDocument
{
    [BsonElement("apelido")]
    public string Apelido { get; set; } = string.Empty;

    [BsonElement("totalDoado")]
    public decimal TotalDoado { get; set; }

    [BsonElement("quantidadeDoacoes")]
    public int QuantidadeDoacoes { get; set; }
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

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;
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

internal sealed class DoacaoAnonimaProjectionDocument
{
    [BsonElement("apelidoDoador")]
    public string ApelidoDoador { get; init; } = string.Empty;

    [BsonElement("valor")]
    public decimal Valor { get; init; }

    [BsonElement("data")]
    public DateTime Data { get; init; }
}