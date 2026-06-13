namespace LoteriaMexicana.Services;

public static class ResolutorRecursosService
{

    public static string ResolverCarpeta(
        string carpetaOriginal,
        string nombreRecurso,
        string extensionArchivos)
    {
        // Si la carpeta original existe y tiene archivos válidos, usarla directamente.
        if (DirectorioExisteYTieneArchivos(carpetaOriginal, extensionArchivos))
            return carpetaOriginal;

        // Intentar encontrar Resources/{nombreRecurso} subiendo desde BaseDirectory.
        var directorioActual = AppDomain.CurrentDomain.BaseDirectory;
        for (int nivel = 0; nivel < 6; nivel++)
        {
            var candidato = Path.Combine(directorioActual, "Resources", nombreRecurso);
            if (DirectorioExisteYTieneArchivos(candidato, extensionArchivos))
                return candidato;

            var directorioPadre = Directory.GetParent(directorioActual);
            if (directorioPadre == null) break;
            directorioActual = directorioPadre.FullName;
        }

        // No se encontró; devolver la original (los métodos manejarán el caso "no existe").
        return carpetaOriginal;
    }

    private static bool DirectorioExisteYTieneArchivos(string ruta, string extension)
        => Directory.Exists(ruta) && Directory.GetFiles(ruta, extension).Length > 0;
}
