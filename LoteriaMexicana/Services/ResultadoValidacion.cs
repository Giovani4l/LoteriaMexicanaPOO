namespace LoteriaMexicana.Services;

public sealed record ResultadoValidacion(
    bool EsVictoria,
    bool HayTrampa,
    IReadOnlyList<int> CartasConTrampa)
{
    public static ResultadoValidacion Victoria() =>
        new(EsVictoria: true, HayTrampa: false, CartasConTrampa: Array.Empty<int>());

    public static ResultadoValidacion Trampa(IEnumerable<int> cartasInvalidas) =>
        new(EsVictoria: false, HayTrampa: true, CartasConTrampa: cartasInvalidas.ToList().AsReadOnly());

    public static ResultadoValidacion FallaLimpia() =>
        new(EsVictoria: false, HayTrampa: false, CartasConTrampa: Array.Empty<int>());
}
