using System.Speech.Synthesis;

namespace LoteriaMexicana.Services;

public sealed class TtsService : IDisposable
{
    private readonly SpeechSynthesizer _sintetizador = new();
    private bool _disposed;

    public bool Habilitado { get; set; } = true;

    public TtsService()
    {
        _sintetizador.SetOutputToDefaultAudioDevice();
        _sintetizador.Rate = -1; // velocidad ligeramente más lenta, más natural

        var vozEnEspaniol = _sintetizador
            .GetInstalledVoices()
            .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("es", StringComparison.OrdinalIgnoreCase));

        if (vozEnEspaniol != null)
            _sintetizador.SelectVoice(vozEnEspaniol.VoiceInfo.Name);
    }

    /// <summary>Lee en voz alta la frase y el nombre de la carta.</summary>
    public void CantarCarta(string frase, string nombreCarta)
    {
        if (!Habilitado) return;
        //_sintetizador.SpeakAsyncCancelAll();
        _sintetizador.SpeakAsync($"{frase}... {nombreCarta}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _sintetizador.SpeakAsyncCancelAll();
        _sintetizador.Dispose();
        _disposed = true;
    }
    public void CambiarVelocidad(int velocidad)
    {
        _sintetizador.Rate = velocidad;
    }
}
