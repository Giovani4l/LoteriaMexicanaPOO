using System.Net;
using System.Net.Sockets;
using LoteriaMexicana.Hubs;

namespace LoteriaMexicana.Services;

public class ServidorSignalR : IAsyncDisposable
{
    private WebApplication? _aplicacionWeb;
    private bool _disposed;

    public const int Puerto = 5050;

    public static string ObtenerIpLocal()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public async Task IniciarAsync()
    {
        AbrirPuertoEnFirewall(Puerto);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddCors(opciones => opciones.AddDefaultPolicy(politica =>
            politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        builder.WebHost.UseUrls($"http://0.0.0.0:{Puerto}");

        _aplicacionWeb = builder.Build();
        _aplicacionWeb.UseCors();
        _aplicacionWeb.MapHub<LoteriaHub>("/loteriahub");

        await _aplicacionWeb.StartAsync();
    }

    private static void AbrirPuertoEnFirewall(int puerto)
    {
        try
        {
            var configuracion = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"LoteriaMexicana\" " +
                                  $"dir=in action=allow protocol=TCP localport={puerto}",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            System.Diagnostics.Process.Start(configuracion)?.WaitForExit(3000);
        }
        catch
        {
            // Si falla, el usuario puede abrir el puerto manualmente
        }
    }

    public async Task DetenerAsync()
    {
        if (_aplicacionWeb != null)
            await _aplicacionWeb.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_aplicacionWeb != null)
        {
            try { await _aplicacionWeb.StopAsync(); }
            catch { /* ignorar errores al detener */ }

            await _aplicacionWeb.DisposeAsync();
            _aplicacionWeb = null;
        }
    }
}
