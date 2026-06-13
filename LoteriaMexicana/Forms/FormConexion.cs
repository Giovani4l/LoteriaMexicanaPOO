using LoteriaMexicana.Services;

namespace LoteriaMexicana.Forms;

public partial class FormConexion : Form
{
    public FormConexion()
    {
        InitializeComponent();
        
    }

    // ── Botón "Crear sala" ────────────────────────────────────────────────────

    private async void btnCrear_Click(object sender, EventArgs e)
    {
        if (!ValidarCampoNombre()) return;

        btnCrear.Enabled = false;
        

        try
        {
            var servidor = new ServidorSignalR();
            await servidor.IniciarAsync();

            var ipLocal = ServidorSignalR.ObtenerIpLocal();

            // La conexión real ocurre en FormJuegoRed.UnirseAsync(),
            // DESPUÉS de suscribir todos los eventos SignalR.
            var cliente = new ClienteSignalR($"http://{ipLocal}:{ServidorSignalR.Puerto}/loteriahub");

            

            MessageBox.Show(
                $"Sala creada correctamente.\n\n" +
                $"Comparte esta IP con los demás jugadores:\n\n" +
                $"     {ipLocal}\n\n" +
                $"Si alguien no puede conectarse:\n" +
                $"- Acepta el permiso de firewall de Windows cuando aparezca.\n" +
                $"- O ejecuta la app como Administrador para que lo configure automáticamente.",
                "Sala lista", MessageBoxButtons.OK, MessageBoxIcon.Information);

            AbrirFormularioJuego(cliente, servidor, txtNombre.Text.Trim(), esHost: true);
        }
        catch (Exception ex)
        {
           
            btnCrear.Enabled = true;
        }
    }

    // ── Botón "Unirse a sala" ─────────────────────────────────────────────────

    private void btnUnirse_Click(object sender, EventArgs e)
    {
        if (!ValidarCampoNombre()) return;

        if (string.IsNullOrWhiteSpace(txtIp.Text))
        {
            MessageBox.Show("Ingresa la IP del Gritón.", "Campo requerido",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnUnirse.Enabled = false;
       

        try
        {
            var cliente = new ClienteSignalR($"http://{txtIp.Text.Trim()}:{ServidorSignalR.Puerto}/loteriahub");
            
            AbrirFormularioJuego(cliente, servidor: null, txtNombre.Text.Trim(), esHost: false);
        }
        catch (Exception ex)
        {
           
            btnUnirse.Enabled = true;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AbrirFormularioJuego(ClienteSignalR cliente, ServidorSignalR? servidor,
                                       string nombreJugador, bool esHost)
    {
        var formularioJuego = new FormJuegoRed(cliente, servidor, nombreJugador, esHost);
        formularioJuego.Show();
        Hide();

        formularioJuego.FormClosed += (_, _) =>
        {
            Show();
            btnCrear.Enabled = true;
            btnUnirse.Enabled = true;
            
        };
    }

    private bool ValidarCampoNombre()
    {
        if (!string.IsNullOrWhiteSpace(txtNombre.Text)) return true;

        MessageBox.Show("Ingresa tu nombre.", "Campo requerido",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private void lblSubtitulo_Click(object sender, EventArgs e) { }
}
