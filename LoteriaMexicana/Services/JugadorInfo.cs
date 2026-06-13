namespace LoteriaMexicana.Services;

public class JugadorInfo
{
    public string Nombre { get; set; } = string.Empty;
    public bool EsHost { get; set; }
    public bool EstaListo { get; set; }
    public int Victorias { get; set; }
    public int Puntos { get; set; }
}
