namespace LoteriaMexicana.Domain;


public class Tabla
{
    // Valores por defecto (5x5); se pueden cambiar al iniciar partida
    public const int FilasDefault = 5;
    public const int ColumnasDefault = 5;

    public int Filas { get; }
    public int Columnas { get; }
    public int TotalCasillas => Filas * Columnas;

    // Propiedades estáticas de conveniencia (usadas por código existente que no tiene instancia)
    // Se actualizan cuando se crea una tabla — útiles solo en servidor single-sala.
    public static int FilasActuales { get; private set; } = FilasDefault;
    public static int ColumnasActuales { get; private set; } = ColumnasDefault;
    public static int TotalCasillasActuales => FilasActuales * ColumnasActuales;

    public Carta[,] Casillas { get; }

    private Tabla(Carta[,] casillas)
    {
        Casillas = casillas;
        Filas = casillas.GetLength(0);
        Columnas = casillas.GetLength(1);
        FilasActuales = Filas;
        ColumnasActuales = Columnas;
    }

    //Crea una tabla vacía con las dimensiones especificadas.
    public static Tabla Vacia(int filas = FilasDefault, int columnas = ColumnasDefault)
        => new(new Carta[filas, columnas]);

    //Crea una tabla desde un arreglo bidimensional de cartas.
    public static Tabla DesdeArreglo(Carta[,] casillas) => new(casillas);


    public static Tabla GenerarAleatoria(
        IEnumerable<Carta> todasLasCartas,
        bool tablaDoble = false,
        int filas = FilasDefault,
        int columnas = ColumnasDefault)
    {
        var cartasSeleccionadas = tablaDoble
            ? SeleccionarCartasConRepeticion(todasLasCartas, filas, columnas)
            : SeleccionarCartasSinRepeticion(todasLasCartas, filas, columnas);

        return ConstruirTabla(cartasSeleccionadas, filas, columnas);
    }

    //Obtiene el índice lineal correspondiente a una celda (fila, columna)
    public static int IndiceDe(int fila, int columna) => fila * ColumnasActuales + columna;

    //Convierte la tabla a una lista de DTOs para serialización JSON.
    public List<TablaJsonDto> AJsonDto()
    {
        var lista = new List<TablaJsonDto>();
        for (int fila = 0; fila < Filas; fila++)
            for (int columna = 0; columna < Columnas; columna++)
            {
                var carta = Casillas[fila, columna];
                if (carta != null)
                    lista.Add(new TablaJsonDto(fila, columna, carta.Numero, carta.Nombre));
            }
        return lista;
    }

    // ─── Métodos privados ────────────────────────────────────────────────────

    //Selecciona cartas sin repetición para la tabla.
    private static Carta[] SeleccionarCartasSinRepeticion(
        IEnumerable<Carta> todasLasCartas,
        int filas,
        int columnas)
    {
        int totalCasillas = filas * columnas;
        return todasLasCartas
            .OrderBy(_ => Random.Shared.Next())
            .Take(totalCasillas)
            .ToArray();
    }

    //Selecciona cartas con una repetición para la tabla.
    private static Carta[] SeleccionarCartasConRepeticion(
        IEnumerable<Carta> todasLasCartas,
        int filas,
        int columnas)
    {
        int totalCasillas = filas * columnas;
        var pool = todasLasCartas.OrderBy(_ => Random.Shared.Next()).ToList();

        // Seleccionar (total-1) cartas únicas
        var cartasUnicas = pool.Take(totalCasillas - 1).ToList();

        // Seleccionar una carta al azar para repetir
        var cartaRepetida = cartasUnicas[Random.Shared.Next(cartasUnicas.Count)];

        // Mezclar cartas únicas + repetición
        return cartasUnicas
            .Append(cartaRepetida)
            .OrderBy(_ => Random.Shared.Next())
            .ToArray();
    }

    //Construye una tabla a partir de un arreglo lineal de cartas.
    private static Tabla ConstruirTabla(Carta[] cartasSeleccionadas, int filas, int columnas)
    {
        var casillas = new Carta[filas, columnas];
        for (int indice = 0; indice < cartasSeleccionadas.Length; indice++)
            casillas[indice / columnas, indice % columnas] = cartasSeleccionadas[indice];

        return new Tabla(casillas);
    }
}

public record TablaJsonDto(int Fila, int Columna, int NumeroCarta, string NombreCarta);
