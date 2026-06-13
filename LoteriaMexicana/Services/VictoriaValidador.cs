using LoteriaMexicana.Domain;
using LoteriaMexicana.Domain.Enums;

namespace LoteriaMexicana.Services;

public static class VictoriaValidador
{
    public static bool EsVictoria(Tabla tabla, IReadOnlySet<int> cartasMarcadas, FormatoGanador formato)
    {
        int filas = tabla.Filas;
        int columnas = tabla.Columnas;
        var indicesMarcados = ObtenerIndicesMarcadosEnTabla(tabla, cartasMarcadas);

        return formato switch
        {
            FormatoGanador.LineaHorizontal => TieneLineaHorizontal(indicesMarcados, filas, columnas),
            FormatoGanador.LineaVertical => TieneLineaVertical(indicesMarcados, filas, columnas),
            FormatoGanador.Diagonal => TieneDiagonal(indicesMarcados, filas, columnas),
            FormatoGanador.Cruz => TieneCruz(indicesMarcados, filas, columnas),
            FormatoGanador.Cruzita => TieneCruzita(indicesMarcados, filas, columnas),
            FormatoGanador.TablaLlena => indicesMarcados.Count == tabla.TotalCasillas,
            _ => false
        };
    }

    public static IEnumerable<int> DetectarTrampa(
        IReadOnlySet<int> cartasMarcadas,
        IEnumerable<int> cartasCantadas)
    {
        var cantadasSet = cartasCantadas.ToHashSet();
        return cartasMarcadas.Where(numero => !cantadasSet.Contains(numero));
    }

    // ─── Patrones ────────────────────────────────────────────────────────────

    private static bool TieneLineaHorizontal(HashSet<int> indicesMarcados, int filas, int columnas)
    {
        for (int fila = 0; fila < filas; fila++)
        {
            if (ObtenerIndicesDelFila(fila, columnas, filas).All(indicesMarcados.Contains))
                return true;
        }
        return false;
    }

    private static bool TieneLineaVertical(HashSet<int> indicesMarcados, int filas, int columnas)
    {
        for (int columna = 0; columna < columnas; columna++)
        {
            if (ObtenerIndicesDelColumna(columna, columnas, filas).All(indicesMarcados.Contains))
                return true;
        }
        return false;
    }

    private static bool TieneDiagonal(HashSet<int> indicesMarcados, int filas, int columnas)
    {
        bool diagonalPrincipal = ObtenerIndicesDiagonalPrincipal(filas, columnas).All(indicesMarcados.Contains);
        bool diagonalSecundaria = ObtenerIndicesDiagonalSecundaria(filas, columnas).All(indicesMarcados.Contains);
        return diagonalPrincipal || diagonalSecundaria;
    }

    private static bool TieneCruz(HashSet<int> indicesMarcados, int filas, int columnas)
    {
        int filaMedia = filas / 2;
        int columnaMedia = columnas / 2;

        bool tieneFilaMedia = ObtenerIndicesDelFila(filaMedia, columnas, filas).All(indicesMarcados.Contains);
        bool tieneColumnaMedia = ObtenerIndicesDelColumna(columnaMedia, columnas, filas).All(indicesMarcados.Contains);
        return tieneFilaMedia && tieneColumnaMedia;
    }

    private static bool TieneCruzita(HashSet<int> indicesMarcados, int filas, int columnas)
    {
        for (int fila = 1; fila < filas - 1; fila++)
            for (int columna = 1; columna < columnas - 1; columna++)
            {
                var indicesCruzita = ObtenerIndicesCruzitaEnCentro(fila, columna, columnas);
                if (indicesCruzita.All(indicesMarcados.Contains))
                    return true;
            }
        return false;
    }

    // ─── Helpers para cálculo de índices ──────────────────────────────────────

    /// <summary>Obtiene los índices de todas las celdas en una fila.</summary>
    private static IEnumerable<int> ObtenerIndicesDelFila(int fila, int columnas, int filas)
        => Enumerable.Range(0, columnas).Select(col => fila * columnas + col);

    /// <summary>Obtiene los índices de todas las celdas en una columna.</summary>
    private static IEnumerable<int> ObtenerIndicesDelColumna(int columna, int columnas, int filas)
        => Enumerable.Range(0, filas).Select(fila => fila * columnas + columna);

    /// <summary>Obtiene los índices de la diagonal principal (esquina superior izquierda a inferior derecha).</summary>
    private static IEnumerable<int> ObtenerIndicesDiagonalPrincipal(int filas, int columnas)
        => Enumerable.Range(0, filas).Select(i => i * columnas + i);

    /// <summary>Obtiene los índices de la diagonal secundaria (esquina superior derecha a inferior izquierda).</summary>
    private static IEnumerable<int> ObtenerIndicesDiagonalSecundaria(int filas, int columnas)
        => Enumerable.Range(0, filas).Select(i => i * columnas + (columnas - 1 - i));

    /// <summary>Obtiene los índices de una cruzita con centro en (fila, columna).</summary>
    private static int[] ObtenerIndicesCruzitaEnCentro(int fila, int columna, int columnas)
        =>
        [
            fila * columnas + columna,              // Centro
            (fila - 1) * columnas + columna,        // Arriba
            (fila + 1) * columnas + columna,        // Abajo
            fila * columnas + (columna - 1),        // Izquierda
            fila * columnas + (columna + 1),        // Derecha
        ];

    /// <summary>Obtiene los índices de las celdas marcadas en la tabla.</summary>
    private static HashSet<int> ObtenerIndicesMarcadosEnTabla(
        Tabla tabla, IReadOnlySet<int> cartasMarcadas)
    {
        var indicesMarcados = new HashSet<int>();

        for (int fila = 0; fila < tabla.Filas; fila++)
            for (int columna = 0; columna < tabla.Columnas; columna++)
            {
                var carta = tabla.Casillas[fila, columna];
                if (carta != null && cartasMarcadas.Contains(carta.Numero))
                    indicesMarcados.Add(fila * tabla.Columnas + columna);
            }

        return indicesMarcados;
    }
}
