using Esperanca.Worker.Service.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Esperanca.Worker.Service.Mongo;

public sealed record ProcessarDoacaoResult(
    bool Processada,
    decimal ValorTotalArrecadado);

public sealed class DoacaoMongoService
{
    private const string DoadorAnonimo = "Doador anônimo";

    private readonly IMongoCollection<DoacaoDocument> _doacoes;
    private readonly IMongoCollection<PainelMacroProjectionDocument> _painelMacro;
    private readonly IMongoCollection<CampanhaListaProjectionDocument> _listaCampanhas;
    private readonly IMongoCollection<CampanhaDetalheProjectionDocument> _campanhaDetalhe;
    private readonly ILogger<DoacaoMongoService> _logger;

    public DoacaoMongoService(
        IMongoClient mongoClient,
        IOptions<MongoOptions> options,
        ILogger<DoacaoMongoService> logger)
    {
        var mongoOptions = options.Value;
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);

        _doacoes = database.GetCollection<DoacaoDocument>(mongoOptions.DoacoesCollection);
        _painelMacro = database.GetCollection<PainelMacroProjectionDocument>(mongoOptions.PainelMacroCollection);
        _listaCampanhas = database.GetCollection<CampanhaListaProjectionDocument>(mongoOptions.ListaCampanhasCollection);
        _campanhaDetalhe = database.GetCollection<CampanhaDetalheProjectionDocument>(mongoOptions.CampanhaDetalheCollection);
        _logger = logger;
    }

    public async Task CriarIndicesAsync(CancellationToken ct)
    {
        var doacaoKeys = Builders<DoacaoDocument>.IndexKeys.Ascending(x => x.IdempotencyKey);
        var doacaoIndex = new CreateIndexModel<DoacaoDocument>(
            doacaoKeys,
            new CreateIndexOptions { Unique = true, Name = "ux_doacoes_idempotency_key" });

        await _doacoes.Indexes.CreateOneAsync(doacaoIndex, cancellationToken: ct);

        var listaKeys = Builders<CampanhaListaProjectionDocument>.IndexKeys.Ascending(x => x.IdCampanha);
        var listaIndex = new CreateIndexModel<CampanhaListaProjectionDocument>(
            listaKeys,
            new CreateIndexOptions { Name = "ix_lista_campanhas_id_campanha" });

        await _listaCampanhas.Indexes.CreateOneAsync(listaIndex, cancellationToken: ct);

        var detalheKeys = Builders<CampanhaDetalheProjectionDocument>.IndexKeys.Ascending(x => x.IdCampanha);
        var detalheIndex = new CreateIndexModel<CampanhaDetalheProjectionDocument>(
            detalheKeys,
            new CreateIndexOptions { Name = "ix_campanha_detalhe_id_campanha" });

        await _campanhaDetalhe.Indexes.CreateOneAsync(detalheIndex, cancellationToken: ct);
    }

    public async Task<ProcessarDoacaoResult> ProcessarDoacaoAsync(DoacaoDocument document, Guid idCampanha, CancellationToken ct)
    {
        if (await ExistePorIdempotencyKeyAsync(document.IdempotencyKey, ct))
        {
            _logger.LogInformation(
                "Doação já processada pelo worker. IdDoacao={IdDoacao}, IdempotencyKey={IdempotencyKey}",
                document.IdDoacao,
                document.IdempotencyKey);

            var detalheExistente = await _campanhaDetalhe
                .Find(x => x.IdCampanha == idCampanha)
                .FirstOrDefaultAsync(ct);

            return new ProcessarDoacaoResult(
                false,
                detalheExistente?.ValorArrecadado ?? 0m);
        }

        await _doacoes.InsertOneAsync(document, cancellationToken: ct);

        await AtualizarListaCampanhasAsync(idCampanha, document, ct);

        var valorTotalArrecadado = await AtualizarDetalheCampanhaAsync(
            idCampanha,
            document,
            ct);

        await AtualizarPainelMacroAsync(document, ct);

        return new ProcessarDoacaoResult(
            true,
            valorTotalArrecadado);
    }

    private async Task<bool> ExistePorIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        return await _doacoes
            .Find(x => x.IdempotencyKey == idempotencyKey)
            .AnyAsync(ct);
    }

    private async Task AtualizarPainelMacroAsync(DoacaoDocument document, CancellationToken ct)
    {
        var campanhas = await _listaCampanhas
            .Find(FilterDefinition<CampanhaListaProjectionDocument>.Empty)
            .ToListAsync(ct);

        var topDoadores = await ObterTopDoadoresAsync(ct);

        var update = Builders<PainelMacroProjectionDocument>.Update
            .Inc(x => x.TotalArrecadado, document.Valor)
            .Inc(x => x.TotalDoacoes, 1)
            .Set(x => x.TotalCampanhasAtivas, campanhas.Count(x => x.Status == "EmAndamento"))
            .Set(x => x.TotalCampanhasConcluidas, campanhas.Count(x => x.Status == "Concluida"))
            .Set(x => x.TopDoadores, topDoadores)
            .Set(x => x.AtualizadoEm, document.DataProcessamento);

        await _painelMacro.UpdateOneAsync(
            FilterDefinition<PainelMacroProjectionDocument>.Empty,
            update,
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    private async Task AtualizarListaCampanhasAsync(Guid idCampanha, DoacaoDocument document, CancellationToken ct)
    {
        var result = await _listaCampanhas.UpdateOneAsync(
            x => x.IdCampanha == idCampanha,
            Builders<CampanhaListaProjectionDocument>.Update
                .Inc(x => x.ValorArrecadado, document.Valor),
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            _logger.LogWarning("Projeção lista_campanhas não encontrada para campanha {IdCampanha}.", idCampanha);
    }

    private async Task<decimal> AtualizarDetalheCampanhaAsync(Guid idCampanha, DoacaoDocument document, CancellationToken ct)
    {
        var doacao = new DoacaoAnonimaProjectionDocument
        {
            ApelidoDoador = DoadorAnonimo,
            Valor = document.Valor,
            Data = document.DataProcessamento
        };

        var projection = await _campanhaDetalhe.FindOneAndUpdateAsync(
            x => x.IdCampanha == idCampanha,
            Builders<CampanhaDetalheProjectionDocument>.Update
                .Inc(x => x.ValorArrecadado, document.Valor)
                .Push(x => x.Doacoes, doacao),
            new FindOneAndUpdateOptions<CampanhaDetalheProjectionDocument>
            {
                ReturnDocument = ReturnDocument.After
            },
            ct);

        if (projection is null)
        {
            _logger.LogWarning(
                "Projeção campanha_detalhe não encontrada para campanha {IdCampanha}.",
                idCampanha);

            return 0m;
        }

        return projection.ValorArrecadado;
    }

    private async Task<List<TopDoadorProjectionDocument>> ObterTopDoadoresAsync(CancellationToken ct)
    {
        var doacoes = await _doacoes
            .Find(FilterDefinition<DoacaoDocument>.Empty)
            .ToListAsync(ct);

        return doacoes
            .Where(x => !string.IsNullOrWhiteSpace(x.IdDoador))
            .GroupBy(x => x.IdDoador)
            .Select(g => new
            {
                TotalDoado = g.Sum(x => x.Valor),
                QuantidadeDoacoes = g.Count()
            })
            .OrderByDescending(x => x.TotalDoado)
            .ThenByDescending(x => x.QuantidadeDoacoes)
            .Take(5)
            .Select((x, index) => new TopDoadorProjectionDocument
            {
                Apelido = $"Doador anônimo #{index + 1}",
                TotalDoado = x.TotalDoado,
                QuantidadeDoacoes = x.QuantidadeDoacoes
            })
            .ToList();
    }
}
