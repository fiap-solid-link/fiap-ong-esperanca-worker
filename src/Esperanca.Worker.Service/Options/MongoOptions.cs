namespace Esperanca.Worker.Service.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";
    public string DatabaseName { get; init; } = "doacoes_db";
    public string DoacoesCollection { get; init; } = "doacoes";
}
