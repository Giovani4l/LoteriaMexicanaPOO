namespace LoteriaMexicana.Domain;

/// <summary>DTO para transferir datos de jugador por SignalR.</summary>
public record JugadorDto(string Nombre, bool EsGriton, int Victorias, int Puntos = 0);
