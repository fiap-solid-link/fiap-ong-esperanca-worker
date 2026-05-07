namespace Esperanca.Worker.Application._Shared.Results;

public class Result<T>
{
    public bool Sucesso { get; }
    public T? Dados { get; }
    public string? Erro { get; }
    public int StatusCode { get; }

    private Result(T dados, int statusCode = 200)
    {
        Sucesso    = true;
        Dados      = dados;
        StatusCode = statusCode;
    }

    private Result(string erro, int statusCode)
    {
        Sucesso    = false;
        Erro       = erro;
        StatusCode = statusCode;
    }

    public static Result<T> Ok(T dados) => new(dados);
    public static Result<T> Created(T dados) => new(dados, 201);
    public static Result<T> Fail(string erro, int statusCode = 400) => new(erro, statusCode);
    public static Result<T> NotFound(string erro) => new(erro, 404);
    public static Result<T> Unauthorized(string erro) => new(erro, 401);
}
