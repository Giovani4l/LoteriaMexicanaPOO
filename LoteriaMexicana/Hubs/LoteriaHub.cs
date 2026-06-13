using LoteriaMexicana.Domain;
using LoteriaMexicana.Domain.Enums;
using LoteriaMexicana.Services;
using Microsoft.AspNetCore.SignalR;

namespace LoteriaMexicana.Hubs;

public record CasillaDto(int Numero, string Nombre);

public class LoteriaHub : Hub
{
    private static readonly object _bloqueo = new();

    private static Baraja _baraja = new();
    private static bool _partidaEnCurso = false;
    private static FormatoGanador _formatoActual = FormatoGanador.Ninguno;
    private static string? _idConexionHost = null;
    private static bool _tablaDobleActiva = false;
    private static int _filasTabla = Tabla.FilasDefault;
    private static int _columnasTabla = Tabla.ColumnasDefault;

    private const int PuntosPorCarta = 10;
    private const int PuntosPorVictoria = 100;

    private static readonly Dictionary<string, JugadorInfo> _jugadoresPorConexion = new();
    private static readonly Dictionary<string, Tabla> _tablasPorConexion = new();
    private static readonly Dictionary<string, HashSet<int>> _marcasPorConexion = new();
    private static readonly HashSet<int> _numerosCantatosEnPartida = new();

    // =========================================================================
    // DESCONEXIÓN
    // =========================================================================

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? idNuevoHost = null;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId))
                goto saltar;

            bool eraElHost = (_idConexionHost == Context.ConnectionId);
            _jugadoresPorConexion.Remove(Context.ConnectionId);
            _tablasPorConexion.Remove(Context.ConnectionId);
            _marcasPorConexion.Remove(Context.ConnectionId);

            if (eraElHost)
            {
                _idConexionHost = _jugadoresPorConexion.Keys.FirstOrDefault();

                if (_idConexionHost != null)
                {
                    _jugadoresPorConexion[_idConexionHost].EsHost = true;
                    idNuevoHost = _idConexionHost;
                }
                else
                {
                    _partidaEnCurso = false;
                    _formatoActual = FormatoGanador.Ninguno;
                    _baraja = new Baraja();
                    _idConexionHost = null;
                    _tablaDobleActiva = false;
                    _filasTabla = Tabla.FilasDefault;
                    _columnasTabla = Tabla.ColumnasDefault;
                    _numerosCantatosEnPartida.Clear();
                    _jugadoresPorConexion.Clear();
                    _tablasPorConexion.Clear();
                    _marcasPorConexion.Clear();
                }
            }
        }
    saltar:

        if (idNuevoHost != null)
            await Clients.Client(idNuevoHost).SendAsync("RolesActualizados", true);

        await BroadcastEstadoJugadores();
        await base.OnDisconnectedAsync(exception);
    }

    // =========================================================================
    // UNIRSE AL JUEGO
    // =========================================================================

    public async Task UnirseAlJuego(string nombreJugador)
    {
        var nombreFinal = string.IsNullOrWhiteSpace(nombreJugador)
            ? "Jugador Anónimo"
            : nombreJugador.Trim();

        bool esHost;
        List<CasillaDto> casillasAsignadas;
        bool hayPartidaEnCurso;
        List<Carta> cartasYaCantadas;
        FormatoGanador formatoVigente;
        int filasTabla;
        int columnasTabla;

        lock (_bloqueo)
        {
            esHost = _jugadoresPorConexion.Count == 0;
            if (esHost) _idConexionHost = Context.ConnectionId;

            _jugadoresPorConexion[Context.ConnectionId] = new JugadorInfo
            {
                Nombre = nombreFinal,
                EsHost = esHost,
                EstaListo = false
            };
            _marcasPorConexion[Context.ConnectionId] = new HashSet<int>();

            var tablaGenerada = Tabla.GenerarAleatoria(
                _baraja.ObtenerTodas(), _tablaDobleActiva, _filasTabla, _columnasTabla);
            _tablasPorConexion[Context.ConnectionId] = tablaGenerada;
            casillasAsignadas = TablaACasillasDto(tablaGenerada);

            hayPartidaEnCurso = _partidaEnCurso;
            cartasYaCantadas = _baraja.CartasCantadas.ToList();
            formatoVigente = _formatoActual;
            filasTabla = _filasTabla;
            columnasTabla = _columnasTabla;
        }

        await Clients.Caller.SendAsync("RolesActualizados", esHost);
        await Clients.Caller.SendAsync("TablaAsignada", casillasAsignadas);

        if (hayPartidaEnCurso && cartasYaCantadas.Any())
        {
            await Clients.Caller.SendAsync("JuegoYaIniciado", formatoVigente.ToString(), casillasAsignadas);
            foreach (var carta in cartasYaCantadas)
                await Clients.Caller.SendAsync("CartaCantada", carta.Numero, carta.Nombre, carta.Frase);
        }

        await BroadcastEstadoJugadores();
    }

   

    public async Task IniciarJuego(
        string formatoTexto,
        bool tablaDoble = false,
        int filas = Tabla.FilasDefault,
        int columnas = Tabla.ColumnasDefault)
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Gritón puede iniciar la partida.");
        }

        if (!Enum.TryParse<FormatoGanador>(formatoTexto, out var formato))
            formato = FormatoGanador.TablaLlena;

        // Validar dimensiones
        filas = filas is 4 or 5 ? filas : Tabla.FilasDefault;
        columnas = columnas is 4 or 5 ? columnas : Tabla.ColumnasDefault;

        Dictionary<string, List<CasillaDto>> casillasParaCadaJugador;

        lock (_bloqueo)
        {
            _baraja = new Baraja();
            _baraja.Barajear();
            _partidaEnCurso = true;
            _formatoActual = formato;
            _tablaDobleActiva = tablaDoble;
            _filasTabla = filas;
            _columnasTabla = columnas;
            _numerosCantatosEnPartida.Clear();

            foreach (var info in _jugadoresPorConexion.Values)
                info.Puntos = 0;

            casillasParaCadaJugador = new Dictionary<string, List<CasillaDto>>();
            foreach (var idConexion in _jugadoresPorConexion.Keys.ToList())
            {
                _marcasPorConexion[idConexion] = new HashSet<int>();
                _tablasPorConexion[idConexion] = Tabla.GenerarAleatoria(
                    _baraja.ObtenerTodas(), tablaDoble, filas, columnas);
                casillasParaCadaJugador[idConexion] = TablaACasillasDto(_tablasPorConexion[idConexion]);
            }
        }

        foreach (var (idConexion, casillas) in casillasParaCadaJugador)
            await Clients.Client(idConexion).SendAsync("TablaAsignada", casillas);

        await Clients.All.SendAsync("JuegoIniciado", formatoTexto);
        await BroadcastEstadoJugadores();
    }

    // =========================================================================
    // CANTAR CARTA — solo el host
    // =========================================================================

    public async Task CantarCarta()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Gritón puede cantar cartas.");
            if (!_partidaEnCurso)
                throw new HubException("El juego no ha iniciado.");
        }

        Carta? cartaSacada;
        bool seRebarajo = false;

        lock (_bloqueo)
        {
            if (!_baraja.TieneCartas)
            {
                _baraja.Barajear();
                seRebarajo = true;
            }
            cartaSacada = _baraja.SacarCarta();
        }

        if (seRebarajo)
            await Clients.All.SendAsync("BarajaRebrajada");

        if (cartaSacada != null)
        {
            List<(string idConexion, int puntosNuevos)> actualizacionesPuntos = new();

            lock (_bloqueo)
            {
                _numerosCantatosEnPartida.Add(cartaSacada.Numero);

                foreach (var (idConexion, marcas) in _marcasPorConexion)
                {
                    if (!_tablasPorConexion.TryGetValue(idConexion, out var tabla)) continue;

                    bool cartaEnTabla = false;
                    for (int f = 0; f < tabla.Filas && !cartaEnTabla; f++)
                        for (int c = 0; c < tabla.Columnas && !cartaEnTabla; c++)
                            if (tabla.Casillas[f, c]?.Numero == cartaSacada.Numero)
                                cartaEnTabla = true;

                    if (cartaEnTabla && marcas.Contains(cartaSacada.Numero))
                    {
                        _jugadoresPorConexion[idConexion].Puntos += PuntosPorCarta;
                        actualizacionesPuntos.Add((idConexion, _jugadoresPorConexion[idConexion].Puntos));
                    }
                }
            }

            await Clients.All.SendAsync("CartaCantada", cartaSacada.Numero, cartaSacada.Nombre, cartaSacada.Frase);

            foreach (var (idConexion, puntosNuevos) in actualizacionesPuntos)
                await Clients.Client(idConexion).SendAsync("PuntosActualizados", puntosNuevos);

            await BroadcastEstadoJugadores();
        }
    }

    // =========================================================================
    // REINICIAR BARAJA
    // =========================================================================

    public async Task ReiniciarBaraja()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Gritón puede reiniciar la baraja.");
        }
        await ReiniciarBarajaInterno();
    }

    // =========================================================================
    // REINICIAR SALA — solo el host
    // Vuelve todo al estado inicial (sin partida en curso, puntajes en 0,
    // formato/tamaño/doble por defecto). Los jugadores conectados permanecen
    // conectados y reciben una tabla nueva; cualquier jugador nuevo puede
    // unirse normalmente con UnirseAlJuego.
    // =========================================================================

    public async Task ReiniciarSala()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Gritón puede reiniciar la sala.");
        }

        Dictionary<string, List<CasillaDto>> casillasParaCadaJugador;

        lock (_bloqueo)
        {
            _baraja = new Baraja();
            _baraja.Barajear();
            _partidaEnCurso = false;
            _formatoActual = FormatoGanador.Ninguno;
            _tablaDobleActiva = false;
            _filasTabla = Tabla.FilasDefault;
            _columnasTabla = Tabla.ColumnasDefault;
            _numerosCantatosEnPartida.Clear();

            foreach (var info in _jugadoresPorConexion.Values)
            {
                info.Puntos = 0;
                info.Victorias = 0;
            }

            casillasParaCadaJugador = new Dictionary<string, List<CasillaDto>>();
            foreach (var idConexion in _jugadoresPorConexion.Keys.ToList())
            {
                _marcasPorConexion[idConexion] = new HashSet<int>();
                _tablasPorConexion[idConexion] = Tabla.GenerarAleatoria(
                    _baraja.ObtenerTodas(), _tablaDobleActiva, _filasTabla, _columnasTabla);
                casillasParaCadaJugador[idConexion] = TablaACasillasDto(_tablasPorConexion[idConexion]);
            }
        }

        foreach (var (idConexion, casillas) in casillasParaCadaJugador)
            await Clients.Client(idConexion).SendAsync("TablaAsignada", casillas);

        await Clients.All.SendAsync("SalaReiniciada");
        await BroadcastEstadoJugadores();
    }

    private async Task ReiniciarBarajaInterno()
    {
        lock (_bloqueo)
        {
            _baraja = new Baraja();
            _baraja.Barajear();
            _numerosCantatosEnPartida.Clear();
        }
        await Clients.All.SendAsync("BarajaReiniciada");
    }

    // =========================================================================
    // TOGGLE CARTA
    // =========================================================================

    public async Task ToggleCarta(int numeroCarta)
    {
        List<int> marcasActualizadas;

        lock (_bloqueo)
        {
            if (!_marcasPorConexion.TryGetValue(Context.ConnectionId, out var marcasJugador))
                return;
            if (!_numerosCantatosEnPartida.Contains(numeroCarta))
                return;

            if (!marcasJugador.Remove(numeroCarta))
                marcasJugador.Add(numeroCarta);

            marcasActualizadas = marcasJugador.ToList();
        }

        await Clients.Caller.SendAsync("MarcasActualizadas", marcasActualizadas);
    }

    // =========================================================================
    // RECLAMAR LOTERÍA
    // =========================================================================

    public async Task ReclamarLoteria()
    {
        string? nombreGanador = null;
        List<int>? cartasConTrampa = null;
        int puntosGanador = 0;

        lock (_bloqueo)
        {
            if (!_partidaEnCurso) return;
            if (!_jugadoresPorConexion.TryGetValue(Context.ConnectionId, out var infoJugador)) return;
            if (!_tablasPorConexion.TryGetValue(Context.ConnectionId, out var tablaJugador)) return;
            if (!_marcasPorConexion.TryGetValue(Context.ConnectionId, out var marcasJugador)) return;

            var marcasReadOnly = (IReadOnlySet<int>)marcasJugador;
            var trampaDetectada = VictoriaValidador
                .DetectarTrampa(marcasReadOnly, _numerosCantatosEnPartida)
                .ToList();

            if (trampaDetectada.Any())
            {
                cartasConTrampa = trampaDetectada;
            }
            else if (VictoriaValidador.EsVictoria(tablaJugador, marcasReadOnly, _formatoActual))
            {
                infoJugador.Victorias++;
                infoJugador.Puntos += PuntosPorVictoria;
                puntosGanador = infoJugador.Puntos;
                nombreGanador = infoJugador.Nombre;
                _partidaEnCurso = false;
            }
        }

        if (cartasConTrampa != null)
        {
            await Clients.Caller.SendAsync("Trampa", cartasConTrampa);
        }
        else if (nombreGanador != null)
        {
            await Clients.Caller.SendAsync("PuntosActualizados", puntosGanador);
            await Clients.All.SendAsync("HayGanador", nombreGanador);
            await BroadcastEstadoJugadores();
            await ReiniciarBarajaInterno();
        }
        else
        {
            await Clients.Caller.SendAsync("FalsaAlarma");
        }
    }

    // =========================================================================
    // CARGAR TABLA DESDE JSON
    // =========================================================================

    public async Task CargarTablaDesdeJson(List<CasillaDto> casillasCliente)
    {
        int totalEsperado;
        lock (_bloqueo)
        {
            totalEsperado = _filasTabla * _columnasTabla;
        }

        if (casillasCliente == null || casillasCliente.Count != totalEsperado)
            throw new HubException($"La tabla debe tener exactamente {totalEsperado} casillas.");

        List<CasillaDto> casillasConfirmadas;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId)) return;

            var casillas = new Domain.Carta[_filasTabla, _columnasTabla];
            var todasLasCartas = _baraja.ObtenerTodas().ToDictionary(c => c.Numero);

            for (int i = 0; i < casillasCliente.Count; i++)
            {
                var dto = casillasCliente[i];
                int fila = i / _columnasTabla;
                int col = i % _columnasTabla;

                if (!todasLasCartas.TryGetValue(dto.Numero, out var carta))
                    throw new HubException($"Carta #{dto.Numero} no existe en la baraja.");

                casillas[fila, col] = carta;
            }

            _tablasPorConexion[Context.ConnectionId] = Tabla.DesdeArreglo(casillas);
            _marcasPorConexion[Context.ConnectionId] = new HashSet<int>();
            casillasConfirmadas = casillasCliente;
        }

        await Clients.Caller.SendAsync("TablaAsignada", casillasConfirmadas);
    }

    // =========================================================================
    // NUEVA TABLA
    // =========================================================================

    public async Task PedirNuevaTabla()
    {
        List<CasillaDto> nuevasCasillas;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId)) return;

            var nuevaTabla = Tabla.GenerarAleatoria(
                _baraja.ObtenerTodas(), _tablaDobleActiva, _filasTabla, _columnasTabla);
            _tablasPorConexion[Context.ConnectionId] = nuevaTabla;
            _marcasPorConexion[Context.ConnectionId] = new HashSet<int>();
            nuevasCasillas = TablaACasillasDto(nuevaTabla);
        }

        await Clients.Caller.SendAsync("TablaAsignada", nuevasCasillas);
    }

    // =========================================================================
    // CHAT
    // =========================================================================

    public async Task EnviarMensaje(string mensajeTexto)
    {
        if (string.IsNullOrWhiteSpace(mensajeTexto)) return;

        var nombreRemitente = _jugadoresPorConexion.TryGetValue(Context.ConnectionId, out var jugador)
            ? jugador.Nombre
            : "Desconocido";

        await Clients.All.SendAsync("MensajeRecibido", nombreRemitente, mensajeTexto.Trim());
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private async Task BroadcastEstadoJugadores()
    {
        List<JugadorDto> listaJugadores;

        lock (_bloqueo)
        {
            listaJugadores = _jugadoresPorConexion.Values
                .Select(j => new JugadorDto(j.Nombre, j.EsHost, j.Victorias, j.Puntos))
                .ToList();
        }

        await Clients.All.SendAsync("JugadoresActualizados", listaJugadores);
    }

    private static List<CasillaDto> TablaACasillasDto(Tabla tabla)
    {
        var casillas = new List<CasillaDto>();

        for (int fila = 0; fila < tabla.Filas; fila++)
            for (int col = 0; col < tabla.Columnas; col++)
            {
                var carta = tabla.Casillas[fila, col];
                casillas.Add(new CasillaDto(carta.Numero, carta.Nombre));
            }

        return casillas;
    }
}
