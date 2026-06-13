namespace LoteriaMexicana.Domain;

public class Carta
{
    public int Numero { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Frase { get; init; } = string.Empty;

    public override string ToString() => $"{Numero} - {Nombre}";
}
