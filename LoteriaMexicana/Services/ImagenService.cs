namespace LoteriaMexicana.Services;

public sealed class ImagenService : IDisposable
{
    private readonly string _carpetaCartas;
    private readonly Dictionary<int, Image> _cacheImagenes = new();
    private bool _disposed;

    public ImagenService(string carpetaCartas)
    {
        _carpetaCartas = ResolutorRecursosService.ResolverCarpeta(
            carpetaOriginal: carpetaCartas,
            nombreRecurso: "Cartas",
            extensionArchivos: "*.jpg");
    }


    public Image? ObtenerImagenCarta(int numeroCarta)
    {
        if (_cacheImagenes.TryGetValue(numeroCarta, out var imagenEnCache))
            return imagenEnCache;

        var rutaArchivo = Path.Combine(_carpetaCartas, $"{numeroCarta}.jpg");
        if (!File.Exists(rutaArchivo)) return null;

        try
        {
            // Cargar en MemoryStream para no dejar el archivo bloqueado.
            using var stream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var imagen = Image.FromStream(ms);
            _cacheImagenes[numeroCarta] = imagen;
            return imagen;
        }
        catch
        {
            return null;
        }
    }

    public string CarpetaEfectiva => _carpetaCartas;

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var imagen in _cacheImagenes.Values)
            imagen.Dispose();
        _cacheImagenes.Clear();
        _disposed = true;
    }
}
