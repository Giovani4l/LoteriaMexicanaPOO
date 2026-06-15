using LoteriaMexicana.Domain;
using LoteriaMexicana.Hubs;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoteriaMexicana.Services;

public class ClienteSignalR : IAsyncDisposable
{
    private HubConnection? _conexionHub;
    private readonly string _urlHub;

    public ClienteSignalR(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "http://" + url;
        if (!url.EndsWith("/loteriahub"))
            url = url.TrimEnd('/') + "/loteriahub";

        _urlHub = url;
    }

    public event Action<bool, string>? RolAsignado;
    public event Action<List<CasillaDto>>? TablaAsignada;
    public event Action<string>? JuegoIniciado;
    public event Action<string, List<CasillaDto>>? JuegoYaIniciado;
    public event Action<int, string, string>? CartaCantada;
    public event Action? BarajaReiniciada;
    public event Action? SalaReiniciada;
    public event Action? BarajaRebrajada;
    public event Action<List<int>>? MarcasActualizadas;
    public event Action<string>? HayGanador;
    public event Action<List<int>>? Trampa;
    public event Action? FalsaAlarma;
    public event Action<List<JugadorDto>>? JugadoresActualizados;
    public event Action<string>? Desconectado;
    public event Action<string, string>? MensajeRecibido;
    public event Action<int>? PuntosActualizados;
    public event Action<string>? FormatoAgregado;

    public bool Conectado => _conexionHub?.State == HubConnectionState.Connected;

    public async Task ConectarAsync()
    {
        _conexionHub = new HubConnectionBuilder()
            .WithUrl(_urlHub)
            .WithAutomaticReconnect()
            .Build();

        _conexionHub.On<bool>("RolesActualizados", esHost =>
            RolAsignado?.Invoke(esHost, ""));

        _conexionHub.On<List<CasillaDto>>("TablaAsignada", casillas =>
            TablaAsignada?.Invoke(casillas));

        _conexionHub.On<string>("JuegoIniciado", formato =>
            JuegoIniciado?.Invoke(formato));

        _conexionHub.On<string, List<CasillaDto>>("JuegoYaIniciado", (formato, casillas) =>
            JuegoYaIniciado?.Invoke(formato, casillas));

        _conexionHub.On<int, string, string>("CartaCantada", (numero, nombre, frase) =>
            CartaCantada?.Invoke(numero, nombre, frase));

        _conexionHub.On("BarajaReiniciada", () =>
            BarajaReiniciada?.Invoke());

        _conexionHub.On("SalaReiniciada", () =>
            SalaReiniciada?.Invoke());

        _conexionHub.On("BarajaRebrajada", () =>
            BarajaRebrajada?.Invoke());

        _conexionHub.On<List<int>>("MarcasActualizadas", marcas =>
            MarcasActualizadas?.Invoke(marcas));

        _conexionHub.On<string>("HayGanador", nombreGanador =>
            HayGanador?.Invoke(nombreGanador));

        _conexionHub.On<List<int>>("Trampa", cartasTramposas =>
            Trampa?.Invoke(cartasTramposas));

        _conexionHub.On("FalsaAlarma", () =>
            FalsaAlarma?.Invoke());

        _conexionHub.On<List<JugadorDto>>("JugadoresActualizados", listaJugadores =>
            JugadoresActualizados?.Invoke(listaJugadores));

        _conexionHub.On<string, string>("MensajeRecibido", (nombre, texto) =>
            MensajeRecibido?.Invoke(nombre, texto));

        _conexionHub.On<int>("PuntosActualizados", puntos =>
            PuntosActualizados?.Invoke(puntos));

        _conexionHub.On<string>("FormatoAgregado", formato =>
            FormatoAgregado?.Invoke(formato));

        _conexionHub.Closed += excepcion =>
        {
            Desconectado?.Invoke(excepcion?.Message ?? "Conexion cerrada.");
            return Task.CompletedTask;
        };

        await _conexionHub.StartAsync();
    }

    public Task UnirseAlJuego(string nombreJugador) => InvocarConArgumento("UnirseAlJuego", nombreJugador);
    public Task IniciarJuego(string formato, bool tablaDoble, int filas, int columnas)
    {
        if (_conexionHub == null || _conexionHub.State != HubConnectionState.Connected)
            return Task.CompletedTask;
        return _conexionHub.InvokeAsync("IniciarJuego", formato, tablaDoble, filas, columnas);
    }

    public Task CantarCarta() => InvocarSinArgumentos("CantarCarta");
    public Task ToggleCarta(int numeroCarta) => InvocarConArgumento("ToggleCarta", numeroCarta);
    public Task ReclamarLoteria() => InvocarSinArgumentos("ReclamarLoteria");
    public Task EnviarMensaje(string mensaje) => InvocarConArgumento("EnviarMensaje", mensaje);
    public Task PedirNuevaTabla() => InvocarSinArgumentos("PedirNuevaTabla");
    public Task ReiniciarBaraja() => InvocarSinArgumentos("ReiniciarBaraja");
    public Task ReiniciarSala() => InvocarSinArgumentos("ReiniciarSala");
    public Task CargarTablaDesdeJson(List<CasillaDto> casillas) => InvocarConArgumento("CargarTablaDesdeJson", casillas);
    public Task AgregarFormato(string formato) => InvocarConArgumento("AgregarFormato", formato);

    private Task InvocarSinArgumentos(string metodoHub)
    {
        if (_conexionHub == null || _conexionHub.State != HubConnectionState.Connected)
            return Task.CompletedTask;
        return _conexionHub.InvokeAsync(metodoHub);
    }

    private Task InvocarConArgumento(string metodoHub, object argumento)
    {
        if (_conexionHub == null || _conexionHub.State != HubConnectionState.Connected)
            return Task.CompletedTask;
        return _conexionHub.InvokeAsync(metodoHub, argumento);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexionHub != null)
            await _conexionHub.DisposeAsync();
    }
}