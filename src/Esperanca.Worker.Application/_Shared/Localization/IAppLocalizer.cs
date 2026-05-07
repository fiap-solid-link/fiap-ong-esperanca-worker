namespace Esperanca.Worker.Application._Shared.Localization;

public interface IAppLocalizer
{
    string this[string code] { get; }
}
