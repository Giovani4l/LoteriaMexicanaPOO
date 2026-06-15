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
        _sintetizador.Rate = -1; // velocidad ligeramente mas lenta, mas natural

        var vozEnEspaniol = _sintetizador
            .GetInstalledVoices()
            .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("es", StringComparison.OrdinalIgnoreCase));

        if (vozEnEspaniol != null)
            _sintetizador.SelectVoice(vozEnEspaniol.VoiceInfo.Name);
    }

    public Task CantarCartaAsync(string frase, string nombreCarta)
    {
        if (!Habilitado || _disposed) return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void AlCompletar(object? sender, SpeakCompletedEventArgs e)
        {
            _sintetizador.SpeakCompleted -= AlCompletar;

            if (e.Error != null)
                tcs.TrySetException(e.Error);
            else
                tcs.TrySetResult();
        }

        _sintetizador.SpeakCompleted += AlCompletar;
        _sintetizador.SpeakAsync($"{frase}... {nombreCarta}");
        return tcs.Task;
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