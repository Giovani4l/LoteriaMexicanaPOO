using LoteriaMexicana.Hubs;

namespace LoteriaMexicana.Domain
{
    public class RecibirJson
    {
        // Esta clase mapea exactamente la estructura de tu archivo JSON
        public class ArchivoTablaDto
        {
            public string NombreJugador { get; set; } = string.Empty;
            public string NombreTabla { get; set; } = string.Empty;
            public string FechaGuardado { get; set; } = string.Empty;
            public List<TablaJugadorDto> Tablas { get; set; } = new();
        }
    }
}
