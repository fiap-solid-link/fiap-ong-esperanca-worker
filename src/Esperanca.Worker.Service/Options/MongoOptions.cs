namespace Esperanca.Worker.Service.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string DatabaseName { get; init; } = "doacoes_db";
    public string DoacoesCollection { get; init; } = "doacoes";
    public string PainelMacroCollection { get; init; } = "painel_macro";
    public string ListaCampanhasCollection { get; init; } = "lista_campanhas";
    public string CampanhaDetalheCollection { get; init; } = "campanha_detalhe";
}
