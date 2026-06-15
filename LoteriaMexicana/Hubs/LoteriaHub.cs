using LoteriaMexicana.Domain;
using LoteriaMexicana.Domain.Enums;
using LoteriaMexicana.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace LoteriaMexicana.Hubs;

public record CasillaDto(int Numero, string Nombre);
public record TablaJugadorDto(int Indice, List<CasillaDto> Casillas);
public record CeldaFormatoDto(int Fila, int Columna);

public class FormatoGanadorPersonalizadoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Base { get; set; } = 1;
    public List<CeldaFormatoDto>? Celdas { get; set; }
    public List<List<CeldaFormatoDto>>? Patrones { get; set; }
    public List<int>? Indices { get; set; }
    public List<List<int>>? PatronesIndices { get; set; }
}

public class ArchivoFormatosGanadorDto
{
    public List<FormatoGanadorPersonalizadoDto> Formatos { get; set; } = new();
}

internal record FormatoPersonalizado(string Nombre, List<HashSet<int>> Patrones);

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
    private static readonly HashSet<FormatoGanador> _formatosActivos = new();
    private static readonly Dictionary<string, FormatoPersonalizado> _formatosPersonalizados = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _formatosPersonalizadosActivos = new(StringComparer.OrdinalIgnoreCase);

    private const int PuntosPorCarta = 10;
    private const int PuntosPorVictoria = 100;

    private static readonly Dictionary<string, JugadorInfo> _jugadoresPorConexion = new();
    private static int _cantidadTablas = 1;
    private static readonly Dictionary<string, List<Tabla>> _tablasPorConexion = new();
    private static readonly Dictionary<string, HashSet<int>> _marcasPorConexion = new();
    private static readonly HashSet<int> _numerosCantatosEnPartida = new();

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? idNuevoHost = null;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId))
                goto saltar;

            bool eraElHost = _idConexionHost == Context.ConnectionId;
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
                    ReiniciarEstadoSalaSinJugadores();
                }
            }
        }
    saltar:

        if (idNuevoHost != null)
            await Clients.Client(idNuevoHost).SendAsync("RolesActualizados", true);

        await BroadcastEstadoJugadores();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task UnirseAlJuego(string nombreJugador)
    {
        var nombreFinal = string.IsNullOrWhiteSpace(nombreJugador)
            ? "Jugador Anonimo"
            : nombreJugador.Trim();

        bool esHost;
        List<TablaJugadorDto> tablasAsignadas;
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

            var tablasGeneradas = GenerarTablas(_cantidadTablas);
            _tablasPorConexion[Context.ConnectionId] = tablasGeneradas;
            tablasAsignadas = TablasADto(tablasGeneradas);

            hayPartidaEnCurso = _partidaEnCurso;
            cartasYaCantadas = _baraja.CartasCantadas.ToList();
            formatoVigente = _formatoActual;
            filasTabla = _filasTabla;
            columnasTabla = _columnasTabla;
        }

        await Clients.Caller.SendAsync("RolesActualizados", esHost);
        await Clients.Caller.SendAsync("TablasAsignadas", tablasAsignadas, filasTabla, columnasTabla);

        if (hayPartidaEnCurso && cartasYaCantadas.Any())
        {
            await Clients.Caller.SendAsync("JuegoYaIniciado", formatoVigente.ToString(), tablasAsignadas, filasTabla, columnasTabla);
            foreach (var carta in cartasYaCantadas)
                await Clients.Caller.SendAsync("CartaCantada", carta.Numero, carta.Nombre, carta.Frase);
        }

        await BroadcastEstadoJugadores();
    }

    public async Task IniciarJuego(
        string formatoTexto,
        bool tablaDoble = false,
        int filas = Tabla.FilasDefault,
        int columnas = Tabla.ColumnasDefault,
        int cantidadTablas = 1)
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Griton puede iniciar la partida.");
        }

        if (!Enum.TryParse<FormatoGanador>(formatoTexto, out var formato))
            formato = FormatoGanador.TablaLlena;

        filas = filas == 4 ? 4 : 5;
        columnas = filas;
        cantidadTablas = Math.Max(1, cantidadTablas);

        Dictionary<string, List<TablaJugadorDto>> tablasParaCadaJugador;

        lock (_bloqueo)
        {
            _baraja = new Baraja();
            _baraja.Barajear();
            _partidaEnCurso = true;
            _formatoActual = formato;
            _formatosActivos.Clear();
            _formatosPersonalizadosActivos.Clear();
            _formatosActivos.Add(formato);
            _tablaDobleActiva = tablaDoble;
            _filasTabla = filas;
            _columnasTabla = columnas;
            _cantidadTablas = cantidadTablas;
            _numerosCantatosEnPartida.Clear();

            foreach (var info in _jugadoresPorConexion.Values)
                info.Puntos = 0;

            tablasParaCadaJugador = new Dictionary<string, List<TablaJugadorDto>>();
            foreach (var idConexion in _jugadoresPorConexion.Keys.ToList())
            {
                _marcasPorConexion[idConexion] = new HashSet<int>();
                _tablasPorConexion[idConexion] = GenerarTablas(cantidadTablas);
                tablasParaCadaJugador[idConexion] = TablasADto(_tablasPorConexion[idConexion]);
            }
        }

        foreach (var (idConexion, tablas) in tablasParaCadaJugador)
            await Clients.Client(idConexion).SendAsync("TablasAsignadas", tablas, filas, columnas);

        await Clients.All.SendAsync("JuegoIniciado", formatoTexto);
        await BroadcastEstadoJugadores();
    }

    public async Task CantarCarta()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Griton puede cantar cartas.");
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

        if (cartaSacada == null) return;

        List<(string idConexion, int puntosNuevos)> actualizacionesPuntos = new();

        lock (_bloqueo)
        {
            _numerosCantatosEnPartida.Add(cartaSacada.Numero);

            foreach (var (idConexion, marcas) in _marcasPorConexion)
            {
                if (!_tablasPorConexion.TryGetValue(idConexion, out var tablas)) continue;

                bool cartaEnTabla = tablas.Any(tabla => TablaContieneCarta(tabla, cartaSacada.Numero));

                if (cartaEnTabla && marcas.Any(marca => marca % 1000 == cartaSacada.Numero))
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

    public async Task ReiniciarBaraja()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Griton puede reiniciar la baraja.");
        }
        await ReiniciarBarajaInterno();
    }

    public async Task ReiniciarSala()
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Griton puede reiniciar la sala.");
        }

        Dictionary<string, List<TablaJugadorDto>> tablasParaCadaJugador;

        lock (_bloqueo)
        {
            _baraja = new Baraja();
            _baraja.Barajear();
            _partidaEnCurso = false;
            _formatoActual = FormatoGanador.Ninguno;
            _formatosActivos.Clear();
            _formatosPersonalizadosActivos.Clear();
            _formatosPersonalizados.Clear();
            _tablaDobleActiva = false;
            _filasTabla = Tabla.FilasDefault;
            _columnasTabla = Tabla.ColumnasDefault;
            _cantidadTablas = 1;
            _numerosCantatosEnPartida.Clear();

            foreach (var info in _jugadoresPorConexion.Values)
            {
                info.Puntos = 0;
                info.Victorias = 0;
            }

            tablasParaCadaJugador = new Dictionary<string, List<TablaJugadorDto>>();
            foreach (var idConexion in _jugadoresPorConexion.Keys.ToList())
            {
                _marcasPorConexion[idConexion] = new HashSet<int>();
                _tablasPorConexion[idConexion] = GenerarTablas(_cantidadTablas);
                tablasParaCadaJugador[idConexion] = TablasADto(_tablasPorConexion[idConexion]);
            }
        }

        foreach (var (idConexion, tablas) in tablasParaCadaJugador)
            await Clients.Client(idConexion).SendAsync("TablasAsignadas", tablas, _filasTabla, _columnasTabla);

        await Clients.All.SendAsync("SalaReiniciada");
        await BroadcastEstadoJugadores();
    }

    public async Task AgregarFormato(string formatoTexto)
    {
        lock (_bloqueo)
        {
            if (Context.ConnectionId != _idConexionHost)
                throw new HubException("Solo el Griton puede agregar formatos.");
            if (!_partidaEnCurso)
                throw new HubException("El juego no ha iniciado.");
        }

        var texto = formatoTexto?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
            throw new HubException("Formato invalido.");

        if (texto.StartsWith("{") || texto.StartsWith("["))
        {
            var formatos = ParsearFormatosPersonalizados(texto);
            List<string> agregados = new();

            lock (_bloqueo)
            {
                foreach (var formatoPersonalizado in formatos)
                {
                    _formatosPersonalizados[formatoPersonalizado.Nombre] = formatoPersonalizado;
                    if (_formatosPersonalizadosActivos.Add(formatoPersonalizado.Nombre))
                        agregados.Add(formatoPersonalizado.Nombre);
                }
            }

            if (agregados.Count == 0)
                throw new HubException("Los formatos del JSON ya estaban activos.");

            foreach (var nombre in agregados)
                await Clients.All.SendAsync("FormatoAgregado", nombre);

            return;
        }

        if (!Enum.TryParse<FormatoGanador>(texto, out var formatoEnum) || formatoEnum == FormatoGanador.Ninguno)
            throw new HubException("Formato invalido.");

        bool agregado;
        lock (_bloqueo)
        {
            agregado = _formatosActivos.Add(formatoEnum);
        }

        if (agregado)
            await Clients.All.SendAsync("FormatoAgregado", formatoEnum.ToString());
    }

    public async Task ToggleCarta(int indiceTabla, int numeroCarta)
    {
        List<int> marcasActualizadas;

        lock (_bloqueo)
        {
            if (!_marcasPorConexion.TryGetValue(Context.ConnectionId, out var marcasJugador))
                return;
            var claveMarca = CrearClaveMarca(indiceTabla, numeroCarta);
            if (!marcasJugador.Remove(claveMarca))
                marcasJugador.Add(claveMarca);

            marcasActualizadas = marcasJugador.ToList();
        }

        await Clients.Caller.SendAsync("MarcasActualizadas", marcasActualizadas);
    }

    public async Task ReclamarLoteria()
    {
        string? nombreGanador = null;
        List<int>? cartasConTrampa = null;
        int puntosGanador = 0;

        lock (_bloqueo)
        {
            if (!_partidaEnCurso) return;
            if (!_jugadoresPorConexion.TryGetValue(Context.ConnectionId, out var infoJugador)) return;
            if (!_tablasPorConexion.TryGetValue(Context.ConnectionId, out var tablasJugador)) return;
            if (!_marcasPorConexion.TryGetValue(Context.ConnectionId, out var marcasJugador)) return;

            var marcasPorTabla = ObtenerMarcasPorTabla(marcasJugador);
            var trampaDetectada = marcasPorTabla.Values
                .SelectMany(marcas => VictoriaValidador.DetectarTrampa(marcas, _numerosCantatosEnPartida))
                .Distinct()
                .ToList();

            if (trampaDetectada.Any())
            {
                cartasConTrampa = trampaDetectada;
            }
            else if (tablasJugador.Select((tabla, indice) => (tabla, indice)).Any(item =>
                VictoriaValidador.EsVictoriaEnCualquierFormato(
                    item.tabla,
                    marcasPorTabla.GetValueOrDefault(item.indice, new HashSet<int>()),
                    _formatosActivos,
                    ObtenerPatronesPersonalizadosActivos())))
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

    public Task CargarTablasDesdeJson(List<TablaJugadorDto> tablasCliente)
    {
        int filas;
        int columnas;
        lock (_bloqueo)
        {
            filas = _filasTabla;
            columnas = _columnasTabla;
        }

        return CargarTablasPersonalizadas(tablasCliente, filas, columnas);
    }

    public async Task CargarTablasPersonalizadas(List<TablaJugadorDto> tablasCliente, int filas, int columnas)
    {
        filas = filas == 4 ? 4 : 5;
        columnas = filas;
        int totalEsperado = filas * columnas;

        if (tablasCliente == null || tablasCliente.Count == 0)
            throw new HubException("La tabla personalizada no contiene cartas.");

        if (tablasCliente.Any(t => t.Casillas == null || t.Casillas.Count != totalEsperado))
            throw new HubException($"Cada tabla debe tener exactamente {totalEsperado} casillas para {filas}x{columnas}.");

        List<TablaJugadorDto> tablasConfirmadas;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId)) return;

            if (_partidaEnCurso && (filas != _filasTabla || columnas != _columnasTabla))
                throw new HubException($"La partida actual usa tablas {_filasTabla}x{_columnasTabla}.");

            if (!_partidaEnCurso)
            {
                _filasTabla = filas;
                _columnasTabla = columnas;
            }

            var todasLasCartas = _baraja.ObtenerTodas().ToDictionary(c => c.Numero);
            var tablas = new List<Tabla>();

            foreach (var tablaDto in tablasCliente.OrderBy(t => t.Indice))
            {
                var casillas = new Domain.Carta[filas, columnas];
                for (int i = 0; i < tablaDto.Casillas.Count; i++)
                {
                    var dto = tablaDto.Casillas[i];
                    int fila = i / columnas;
                    int col = i % columnas;

                    if (!todasLasCartas.TryGetValue(dto.Numero, out var carta))
                        throw new HubException($"Carta #{dto.Numero} no existe en la baraja.");

                    casillas[fila, col] = carta;
                }
                tablas.Add(Tabla.DesdeArreglo(casillas));
            }

            var firmasNuevas = tablas.Select(ObtenerFirmaTabla).ToList();
            if (firmasNuevas.Count != firmasNuevas.Distinct().Count())
                throw new HubException("La tabla personalizada contiene tablas repetidas.");

            var firmasOcupadas = _tablasPorConexion
                .Where(par => par.Key != Context.ConnectionId)
                .SelectMany(par => par.Value)
                .Select(ObtenerFirmaTabla)
                .ToHashSet();

            if (firmasNuevas.Any(firmasOcupadas.Contains))
                throw new HubException("Esa tabla ya fue cargada por otro jugador. Carga una tabla diferente.");

            _tablasPorConexion[Context.ConnectionId] = tablas;
            _cantidadTablas = tablas.Count;
            _marcasPorConexion[Context.ConnectionId] = new HashSet<int>();
            tablasConfirmadas = TablasADto(tablas);
        }

        await Clients.Caller.SendAsync("TablasAsignadas", tablasConfirmadas, filas, columnas);
    }

    public async Task PedirNuevaTabla()
    {
        List<TablaJugadorDto> nuevasTablas;

        lock (_bloqueo)
        {
            if (!_jugadoresPorConexion.ContainsKey(Context.ConnectionId)) return;

            var tablas = GenerarTablas(_cantidadTablas);
            _tablasPorConexion[Context.ConnectionId] = tablas;
            _marcasPorConexion[Context.ConnectionId] = new HashSet<int>();
            nuevasTablas = TablasADto(tablas);
        }

        await Clients.Caller.SendAsync("TablasAsignadas", nuevasTablas, _filasTabla, _columnasTabla);
    }

    public async Task EnviarMensaje(string mensajeTexto)
    {
        if (string.IsNullOrWhiteSpace(mensajeTexto)) return;

        var nombreRemitente = _jugadoresPorConexion.TryGetValue(Context.ConnectionId, out var jugador)
            ? jugador.Nombre
            : "Desconocido";

        await Clients.All.SendAsync("MensajeRecibido", nombreRemitente, mensajeTexto.Trim());
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

    private static List<FormatoPersonalizado> ParsearFormatosPersonalizados(string json)
    {
        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<FormatoGanadorPersonalizadoDto>? dtos;

        try
        {
            var limpio = json.Trim();
            if (limpio.StartsWith("["))
            {
                dtos = JsonSerializer.Deserialize<List<FormatoGanadorPersonalizadoDto>>(limpio, opciones);
            }
            else
            {
                var archivo = JsonSerializer.Deserialize<ArchivoFormatosGanadorDto>(limpio, opciones);
                dtos = archivo?.Formatos is { Count: > 0 }
                    ? archivo.Formatos
                    : [JsonSerializer.Deserialize<FormatoGanadorPersonalizadoDto>(limpio, opciones)!];
            }
        }
        catch (JsonException ex)
        {
            throw new HubException($"El JSON no se pudo leer: {ex.Message}");
        }

        if (dtos == null || dtos.Count == 0 || dtos.Any(f => f == null))
            throw new HubException("El JSON no contiene formatos validos.");

        int filas;
        int columnas;
        lock (_bloqueo)
        {
            filas = _filasTabla;
            columnas = _columnasTabla;
        }

        var formatos = dtos.Select(dto => CrearFormatoPersonalizado(dto, filas, columnas)).ToList();
        var nombresDuplicados = formatos
            .GroupBy(f => f.Nombre, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (nombresDuplicados.Count > 0)
            throw new HubException($"El JSON contiene nombres repetidos: {string.Join(", ", nombresDuplicados)}.");

        return formatos;
    }

    private static FormatoPersonalizado CrearFormatoPersonalizado(FormatoGanadorPersonalizadoDto dto, int filas, int columnas)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new HubException("Cada formato debe tener nombre.");

        var patrones = new List<HashSet<int>>();

        if (dto.Patrones is { Count: > 0 })
            patrones.AddRange(dto.Patrones.Select(p => ConvertirCeldasAIndices(p, dto.Base, filas, columnas)));
        else if (dto.Celdas is { Count: > 0 })
            patrones.Add(ConvertirCeldasAIndices(dto.Celdas, dto.Base, filas, columnas));

        if (dto.PatronesIndices is { Count: > 0 })
            patrones.AddRange(dto.PatronesIndices.Select(p => ConvertirIndices(p, dto.Base, filas * columnas)));
        else if (dto.Indices is { Count: > 0 })
            patrones.Add(ConvertirIndices(dto.Indices, dto.Base, filas * columnas));

        if (patrones.Count == 0 || patrones.Any(p => p.Count == 0))
            throw new HubException($"El formato {nombre} no tiene patron valido.");

        var patronesUnicos = patrones
            .Select(p => p.ToHashSet())
            .GroupBy(p => string.Join(",", p.Order()))
            .Select(g => g.First())
            .ToList();

        return new FormatoPersonalizado(nombre, patronesUnicos);
    }

    private static HashSet<int> ConvertirCeldasAIndices(IEnumerable<CeldaFormatoDto> celdas, int baseIndice, int filas, int columnas)
    {
        var indices = new HashSet<int>();
        foreach (var celda in celdas)
        {
            int fila = celda.Fila - baseIndice;
            int columna = celda.Columna - baseIndice;
            if (fila < 0 || fila >= filas || columna < 0 || columna >= columnas)
                throw new HubException($"La celda ({celda.Fila}, {celda.Columna}) esta fuera de la tabla {filas}x{columnas}.");

            indices.Add(fila * columnas + columna);
        }

        return indices;
    }

    private static HashSet<int> ConvertirIndices(IEnumerable<int> indices, int baseIndice, int totalCasillas)
    {
        var resultado = new HashSet<int>();
        foreach (var indiceOriginal in indices)
        {
            int indice = indiceOriginal - baseIndice;
            if (indice < 0 || indice >= totalCasillas)
                throw new HubException($"El indice {indiceOriginal} esta fuera de la tabla.");

            resultado.Add(indice);
        }

        return resultado;
    }

    private static List<IReadOnlySet<int>> ObtenerPatronesPersonalizadosActivos()
    {
        return _formatosPersonalizadosActivos
            .Where(_formatosPersonalizados.ContainsKey)
            .SelectMany(nombre => _formatosPersonalizados[nombre].Patrones)
            .Select(patron => (IReadOnlySet<int>)patron)
            .ToList();
    }

    private static List<Tabla> GenerarTablas(int cantidad)
    {
        var tablas = new List<Tabla>();
        for (int i = 0; i < Math.Max(1, cantidad); i++)
            tablas.Add(Tabla.GenerarAleatoria(_baraja.ObtenerTodas(), _tablaDobleActiva, _filasTabla, _columnasTabla));
        return tablas;
    }

    private static List<TablaJugadorDto> TablasADto(List<Tabla> tablas)
        => tablas.Select((tabla, indice) => new TablaJugadorDto(indice, TablaACasillasDto(tabla))).ToList();

    private static bool TablaContieneCarta(Tabla tabla, int numeroCarta)
    {
        for (int f = 0; f < tabla.Filas; f++)
            for (int c = 0; c < tabla.Columnas; c++)
                if (tabla.Casillas[f, c]?.Numero == numeroCarta)
                    return true;
        return false;
    }

    private static string ObtenerFirmaTabla(Tabla tabla)
    {
        var numeros = new List<int>(tabla.TotalCasillas);
        for (int fila = 0; fila < tabla.Filas; fila++)
            for (int columna = 0; columna < tabla.Columnas; columna++)
                numeros.Add(tabla.Casillas[fila, columna]?.Numero ?? 0);

        return $"{tabla.Filas}x{tabla.Columnas}:" + string.Join(",", numeros);
    }

    private static int CrearClaveMarca(int indiceTabla, int numeroCarta) => indiceTabla * 1000 + numeroCarta;

    private static Dictionary<int, HashSet<int>> ObtenerMarcasPorTabla(IEnumerable<int> marcas)
    {
        var resultado = new Dictionary<int, HashSet<int>>();
        foreach (var marca in marcas)
        {
            int indiceTabla = marca / 1000;
            int numeroCarta = marca % 1000;
            if (!resultado.TryGetValue(indiceTabla, out var cartas))
            {
                cartas = new HashSet<int>();
                resultado[indiceTabla] = cartas;
            }
            cartas.Add(numeroCarta);
        }
        return resultado;
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

    private static void ReiniciarEstadoSalaSinJugadores()
    {
        _partidaEnCurso = false;
        _formatoActual = FormatoGanador.Ninguno;
        _baraja = new Baraja();
        _idConexionHost = null;
        _tablaDobleActiva = false;
        _filasTabla = Tabla.FilasDefault;
        _columnasTabla = Tabla.ColumnasDefault;
        _cantidadTablas = 1;
        _formatosActivos.Clear();
        _formatosPersonalizadosActivos.Clear();
        _formatosPersonalizados.Clear();
        _numerosCantatosEnPartida.Clear();
        _jugadoresPorConexion.Clear();
        _tablasPorConexion.Clear();
        _marcasPorConexion.Clear();
    }
}