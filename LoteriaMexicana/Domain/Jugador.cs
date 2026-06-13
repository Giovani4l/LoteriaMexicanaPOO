namespace LoteriaMexicana.Domain;

public class Jugador
{
    public string Nombre { get; }
    public Tabla Tabla { get; private set; } = Tabla.Vacia();
    public int Victorias { get; private set; }
    public bool EsGriton { get; set; }
    public int Puntos { get; private set; }

    private readonly HashSet<int> _cartasMarcadas = new();
    public IReadOnlySet<int> CartasMarcadas => _cartasMarcadas;

    public Jugador(string nombre) => Nombre = nombre;

    public void AsignarTabla(Tabla nuevaTabla) => Tabla = nuevaTabla;
    public void MarcarCarta(int numeroCarta) => _cartasMarcadas.Add(numeroCarta);
    public void DesmarcarCarta(int numeroCarta) => _cartasMarcadas.Remove(numeroCarta);
    public void LimpiarTodasLasMarcas() => _cartasMarcadas.Clear();
    public void RegistrarVictoria() => Victorias++;
    public void AgregarPuntos(int cantidad) => Puntos += cantidad;
    public void ReiniciarPuntosPorPartida() => Puntos = 0;

    public override string ToString() => Nombre;
}
