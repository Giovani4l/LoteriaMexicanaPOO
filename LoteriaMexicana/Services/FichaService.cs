namespace LoteriaMexicana.Services;


public sealed class FichaService : IDisposable
{
    private readonly string _carpetaFichas;
    private readonly Dictionary<string, Image> _cacheImagenes = new();
    private bool _disposed;

    public static readonly string[] NombresFichas = { "CocaCola", "Corona", "Heineken" };

    public FichaService(string carpetaFichas)
    {
        _carpetaFichas = ResolutorRecursosService.ResolverCarpeta(
            carpetaOriginal: carpetaFichas,
            nombreRecurso: "Fichas",
            extensionArchivos: "*.png");
    }

    public void PrecargarTodasLasFichas()
    {
        foreach (var nombre in NombresFichas)
            ObtenerFicha(nombre);
    }


    public Image? ObtenerFicha(string nombreFicha)
    {
        if (_cacheImagenes.TryGetValue(nombreFicha, out var imagenEnCache))
            return imagenEnCache;

        var rutaArchivo = Path.Combine(_carpetaFichas, $"{nombreFicha}.png");
        if (!File.Exists(rutaArchivo)) return null;

        try
        {
            // Cargar en MemoryStream para no dejar el archivo bloqueado.
            using var stream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var imagen = Image.FromStream(ms);
            _cacheImagenes[nombreFicha] = imagen;
            return imagen;
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyDictionary<string, Image> ObtenerTodasLasFichas() => _cacheImagenes;

    public string CarpetaEfectiva => _carpetaFichas;

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var imagen in _cacheImagenes.Values)
            imagen.Dispose();
        _cacheImagenes.Clear();
        _disposed = true;
    }
}
