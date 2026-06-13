using LoteriaMexicana.Domain;
using LoteriaMexicana.Domain.Enums;

namespace LoteriaMexicana.Services;

public class JuegoService
{
    private readonly Baraja _baraja = new();
    private readonly List<Jugador> _jugadores = new();

    public bool JuegoActivo { get; private set; }
    public FormatoGanador FormatoActual { get; private set; } = FormatoGanador.Ninguno;
    public Carta? UltimaCantada { get; private set; }
    public Jugador? Griton { get; private set; }

    public IReadOnlyList<Jugador> Jugadores => _jugadores.AsReadOnly();
    public IReadOnlyList<Carta> CartasCantadas => _baraja.CartasCantadas;
    public int CartasRestantes => _baraja.CartasRestantes;

    //Añade un jugador con el nombre especificado (espacios trimados).
    public void AgregarJugador(string nombre)
        => _jugadores.Add(new Jugador(nombre.Trim()));


    public void AsignarGriton(Jugador nuevoGriton)
    {
        LimpiarGritonAnterior();
        nuevoGriton.EsGriton = true;
        Griton = nuevoGriton;
    }

    //Configura el formato de ganador para esta partida (línea, diagonal, tabla llena, etc.).
    public void ConfigurarFormato(FormatoGanador formato) => FormatoActual = formato;
    public void IniciarPartida()
    {
        ReiniciarBaraja();
        LimpiarMarcasDelTodos();
    }

    public void IniciarPartidaSinTablas()
    {
        ReiniciarBaraja();
    }

    public Carta? CantarSiguienteCarta()
    {
        if (!JuegoActivo) return null;
        UltimaCantada = _baraja.SacarCarta();
        return UltimaCantada;
    }

    //Genera una tabla aleatoria para el jugador especificado.
    public void GenerarTablaParaJugador(Jugador jugador)
        => jugador.AsignarTabla(Tabla.GenerarAleatoria(_baraja.ObtenerTodas()));

    //Genera tablas aleatorias para todos los jugadores en la partida.
    public void GenerarTablasParaTodos()
    {
        foreach (var jugador in _jugadores)
            GenerarTablaParaJugador(jugador);
    }

    //Marca una carta en la tabla del jugador especificado.
    public void MarcarCarta(Jugador jugador, int numeroCarta)
        => jugador.MarcarCarta(numeroCarta);

    //Desmarca una carta en la tabla del jugador especificado.
    public void DesmarcarCarta(Jugador jugador, int numeroCarta)
        => jugador.DesmarcarCarta(numeroCarta);


    public ResultadoValidacion ValidarVictoria(Jugador jugador)
    {
        var cartasConTrampa = ObtenerCartasConTrampa(jugador);
        if (cartasConTrampa.Count > 0)
            return ResultadoValidacion.Trampa(cartasConTrampa);

        return VictoriaValidador.EsVictoria(jugador.Tabla, jugador.CartasMarcadas, FormatoActual)
            ? ResultadoValidacion.Victoria()
            : ResultadoValidacion.FallaLimpia();
    }


    public void TerminarPartida(Jugador ganador)
    {
        ganador.RegistrarVictoria();
        JuegoActivo = false;
    }

    // ─── Métodos privados ────────────────────────────────────────────────────

    //Reinicia la baraja y prepara el estado para una nueva partida.
    private void ReiniciarBaraja()
    {
        _baraja.Barajear();
        UltimaCantada = null;
        JuegoActivo = true;
    }

    //Limpia el rol de gritón del jugador anterior (si lo hay).
    private void LimpiarGritonAnterior()
    {
        if (Griton != null)
            Griton.EsGriton = false;
    }

    //Limpia las marcas de cartas de todos los jugadores.
    private void LimpiarMarcasDelTodos()
    {
        foreach (var jugador in _jugadores)
            jugador.LimpiarTodasLasMarcas();
    }

    //Obtiene las cartas que el jugador marcó pero no fueron cantadas (trampa).
    private List<int> ObtenerCartasConTrampa(Jugador jugador)
    {
        return VictoriaValidador
            .DetectarTrampa(jugador.CartasMarcadas, _baraja.CartasCantadas.Select(c => c.Numero))
            .ToList();
    }
}
