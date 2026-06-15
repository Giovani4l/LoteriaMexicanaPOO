using System.Drawing.Drawing2D;
using System.Text.Json;
using LoteriaMexicana.Domain;
using LoteriaMexicana.Domain.Enums;
using LoteriaMexicana.Hubs;
using LoteriaMexicana.Services;

namespace LoteriaMexicana.Forms;

public partial class FormJuegoRed : Form
{
    // ── Servicios ─────────────────────────────────────────────────────────────
    private readonly ClienteSignalR _cliente;
    private readonly ServidorSignalR? _servidor;
    private readonly TtsService _tts;
    private readonly ImagenService _imagenes;
    private readonly FichaService _fichas;
    private readonly string _nombreJugador;
    private readonly bool _esHost;
    private System.Windows.Forms.Timer _timerAutoCanto = new();
    private bool _modoAutomatico = false;
    // ── Estado de la partida ──────────────────────────────────────────────────
    private readonly Dictionary<int, string> _fichasEnTabla = new();
    private readonly HashSet<int> _cartasCantadas = new();
    private bool _partidaEnCurso;
    private int _puntos;

    // ── Configuración (se actualiza desde FormConfigurarPartida) ──────────────
    private FormatoGanador _formato = FormatoGanador.TablaLlena;
    private bool _tablaDoble = false;
    private int _filas = 5;
    private int _columnas = 5;
    private readonly HashSet<FormatoGanador> _formatosActivos = new();
    // ── Tabla ─────────────────────────────────────────────────────────────────
    private List<CasillaDto> _casillas = new();
    private int _anchoCol = 110;
    private int _altoFila = 128;
    private const int TamFichaPanel = 70;

    // ── Fichas ────────────────────────────────────────────────────────────────
    private string _fichaActiva = string.Empty;
    private readonly Dictionary<string, PictureBox> _picturesFichas = new();
    private readonly List<ToolTip> _tooltips = new();

    private bool _cantandoCarta = false;
    public FormJuegoRed(ClienteSignalR cliente, ServidorSignalR? servidor,
                        string nombreJugador, bool esHost)
    {
        _cliente = cliente;
        _servidor = servidor;
        _nombreJugador = nombreJugador;
        _esHost = esHost;

        var base_ = AppDomain.CurrentDomain.BaseDirectory;
        _tts = new TtsService();
        _imagenes = new ImagenService(Path.Combine(base_, "Resources", "Cartas"));
        _fichas = new FichaService(Path.Combine(base_, "Resources", "Fichas"));
        _fichas.PrecargarTodasLasFichas();

        InitializeComponent();
        cmbFormatoExtra.DataSource =Enum.GetValues(typeof(FormatoGanador))
        .Cast<FormatoGanador>()
        .Where(f => f != FormatoGanador.Ninguno)
        .ToList();
        _timerAutoCanto.Tick += async (s, e) =>
        {
            _timerAutoCanto.Stop();

            if (_esHost && _partidaEnCurso && !_cantandoCarta)
                await _cliente.CantarCarta();
        };
        Text = $"Lotería Mexicana — {nombreJugador}{(esHost ? " (Gritón)" : "")}";
        lblUsuario.Text = $" {nombreJugador}{(esHost ? "   Gritón" : "")}";

        if (esHost)
        {
            pnlConfigHost.Visible = true;
            btnCantarCarta.Visible = true;
            btnReiniciarSala.Visible = true;
            lblSalaInfo.Text = $"  {ServidorSignalR.ObtenerIpLocal()}:{ServidorSignalR.Puerto}";
        }

        ConstruirPanelFichas();
        SuscribirEventosSignalR();
        VerificarRecursosCartas();

        Shown += async (_, _) => await ConectarAsync();
    }

    private async void btnIniciar_Click(object sender, EventArgs e)
    {
        using var dlg = new FormConfigurarPartida(_formato, _filas, _tablaDoble);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _formato = dlg.FormatoSeleccionado;
        _filas = dlg.Filas;
        _columnas = dlg.Columnas;
        _tablaDoble = dlg.TablaDoble;
        _formatosActivos.Clear();
        _formatosActivos.Add(_formato);

        try
        {
            btnIniciar.Enabled = false;
            await _cliente.IniciarJuego(_formato.ToString(), _tablaDoble, _filas, _columnas);
        }
        catch (Exception ex)
        {
            btnIniciar.Enabled = true;
            MostrarMensaje($"No se pudo iniciar: {ex.Message}", Color.Red);
            MessageBox.Show(ex.Message, "Error al iniciar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void btnCantarCarta_Click(object sender, EventArgs e)
    {
        try { await _cliente.CantarCarta(); }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}", Color.Red); }
    }

    private async void btnLoteria_Click(object sender, EventArgs e)
    {
        try { await _cliente.ReclamarLoteria(); }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}", Color.Red); }
    }

    private async void btnNuevaTabla_Click(object sender, EventArgs e)
    {
        try
        {
            btnNuevaTabla.Enabled = false;
            await _cliente.PedirNuevaTabla();
        }
        catch (Exception ex)
        {
            btnNuevaTabla.Enabled = true;
            MostrarMensaje($"Error: {ex.Message}", Color.Red);
        }
    }

    private async void btnReiniciarSala_Click(object sender, EventArgs e)
    {
        var r = MessageBox.Show(
            "Esto reiniciará la partida y los puntajes.\n\n¿Continuar?",
            "Reiniciar Sala", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) return;

        try { await _cliente.ReiniciarSala(); }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}", Color.Red); }
    }

    private void btnGuardarTabla_Click(object sender, EventArgs e)
    {
        if (_casillas.Count == 0)
        {
            MessageBox.Show("Aún no tienes una tabla asignada.", "Sin tabla",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlgNombre = new FormNombreTabla();
        if (dlgNombre.ShowDialog(this) != DialogResult.OK) return;
        var nombre = dlgNombre.NombreIngresado;

        int cols = (int)Math.Round(Math.Sqrt(_casillas.Count));
        var dto = new TablaGuardadaDto(
            _nombreJugador, nombre,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            _casillas.Select((c, i) => new TablaJsonDto(i / cols, i % cols, c.Numero, c.Nombre)).ToList());

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

        using var dlg = new SaveFileDialog
        {
            Title = "Guardar tabla",
            Filter = "JSON (*.json)|*.json",
            FileName = $"tabla_{nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.json",
            DefaultExt = "json",
            RestoreDirectory = true,
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dlg.FileName, json);
            MostrarMensaje($"✅ Guardada: {Path.GetFileName(dlg.FileName)}", Color.FromArgb(0, 104, 56));
        }
    }

    private void btnCargarTabla_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Cargar tabla",
            Filter = "JSON (*.json)|*.json",
            RestoreDirectory = true,
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        TablaGuardadaDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TablaGuardadaDto>(
                File.ReadAllText(dlg.FileName),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            MessageBox.Show("El archivo no es una tabla válida.", "Error al cargar",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int esperado = _filas * _columnas;
        if (dto?.Casillas == null || dto.Casillas.Count != esperado)
        {
            MessageBox.Show($"La tabla debe tener {esperado} casillas ({_filas}×{_columnas}).",
                "Tabla inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ordenadas = dto.Casillas
            .OrderBy(c => c.Fila).ThenBy(c => c.Columna)
            .Select(c => new CasillaDto(c.NumeroCarta, c.NombreCarta)).ToList();

        RenderizarTabla(ordenadas);
        _ = _cliente.CargarTablaDesdeJson(ordenadas);
        MostrarMensaje($"📂 Tabla \"{dto.NombreTabla}\" de {dto.NombreJugador} cargada.", Color.FromArgb(0, 104, 56));
    }

    private async void btnEnviar_Click(object sender, EventArgs e) => await EnviarMensajeChat();



    private void chkTts_CheckedChanged(object sender, EventArgs e) => _tts.Habilitado = chkTts.Checked;

    private async void txtMensaje_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.SuppressKeyPress = true;
        await EnviarMensajeChat();
    }

    private async void FormJuegoRed_FormClosed(object sender, FormClosedEventArgs e)
    {
        foreach (var t in _tooltips) t.Dispose();
        _tooltips.Clear();
        _tts.Dispose();
        _imagenes.Dispose();
        _fichas.Dispose();
        await _cliente.DisposeAsync();
        if (_servidor != null) await _servidor.DisposeAsync();
    }

    private void lblFrase_Click(object sender, EventArgs e) { }

    // =========================================================================
    // FICHAS (panel lateral)
    // =========================================================================

    private void ConstruirPanelFichas()
    {
        flpFichas.Controls.Clear();
        _picturesFichas.Clear();

        foreach (var nombre in FichaService.NombresFichas)
        {
            var img = _fichas.ObtenerFicha(nombre);
            if (img == null) continue;

            var pic = new PictureBox
            {
                Size = new Size(TamFichaPanel, TamFichaPanel),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = img,
                Cursor = Cursors.Hand,
                Margin = new Padding(4),
                Tag = nombre,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.FixedSingle,
            };
            var tip = new ToolTip();
            tip.SetToolTip(pic, nombre);
            _tooltips.Add(tip);

            pic.Click += pictureFicha_Click;
            _picturesFichas[nombre] = pic;
            flpFichas.Controls.Add(pic);
        }

        if (_picturesFichas.Count > 0)
            SeleccionarFicha(_picturesFichas.Keys.First());
    }

    private void pictureFicha_Click(object sender, EventArgs e)
    {
        if (sender is PictureBox pic && pic.Tag is string nombre)
            SeleccionarFicha(nombre);
    }

    private void SeleccionarFicha(string nombre)
    {
        _fichaActiva = nombre;
        foreach (var (n, pic) in _picturesFichas)
            pic.BackColor = n == nombre ? Color.FromArgb(240, 185, 11) : Color.Transparent;
    }

    // =========================================================================
    // TABLA (grilla de cartas)
    // =========================================================================

    private void RenderizarTabla(List<CasillaDto> casillas)
    {
        int total = casillas.Count;
        int cols = total == 16 ? 4 : 5;
        _anchoCol = cols == 4 ? 137 : 110;
        _altoFila = cols == 4 ? 160 : 128;

        grilla.SuspendLayout();
        grilla.Controls.Clear();
        grilla.ColumnCount = cols;
        grilla.RowCount = cols;
        grilla.AutoSize = false;
        grilla.Size = new Size(cols * _anchoCol, cols * _altoFila);
        grilla.ColumnStyles.Clear();
        grilla.RowStyles.Clear();
        for (int i = 0; i < cols; i++)
        {
            grilla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _anchoCol));
            grilla.RowStyles.Add(new RowStyle(SizeType.Absolute, _altoFila));
        }

        _fichasEnTabla.Clear();
        _casillas = casillas;

        for (int i = 0; i < total; i++)
            grilla.Controls.Add(CrearCelda(casillas[i].Numero, casillas[i].Nombre), i % cols, i / cols);

        grilla.ResumeLayout(true);
        grilla.Location = new Point(Math.Max(0, (pnlGrilla.ClientSize.Width - grilla.Width) / 2), 248);
    }

    private PictureBox CrearCelda(int numero, string nombre)
    {
        var celda = new PictureBox
        {
            Size = new Size(_anchoCol, _altoFila),
            SizeMode = PictureBoxSizeMode.Normal,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Margin = new Padding(0),
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
            Tag = numero,
            Image = EscalarImagen(_imagenes.ObtenerImagenCarta(numero), _anchoCol, _altoFila),
        };
        var tip = new ToolTip();
        tip.SetToolTip(celda, $"{numero} - {nombre}");
        _tooltips.Add(tip);
        celda.Click += celda_Click;
        return celda;
    }

    private void celda_Click(object sender, EventArgs e)
    {
        if (sender is not PictureBox celda || !_partidaEnCurso) return;
        int num = (int)celda.Tag!;

        if (!_cartasCantadas.Contains(num))
        {
            MostrarMensaje("Esa carta todavia no ha salido.", Color.OrangeRed);
            return;
        }

        if (_fichasEnTabla.TryGetValue(num, out var actual) && actual == _fichaActiva)
            _fichasEnTabla.Remove(num);
        else
            _fichasEnTabla[num] = _fichaActiva;

        RefrescarCelda(celda, num);
        _ = _cliente.ToggleCarta(num);
    }

    private void RefrescarCelda(PictureBox celda, int num)
    {
        var carta = _imagenes.ObtenerImagenCarta(num);
        celda.Image = _fichasEnTabla.TryGetValue(num, out var ficha)
            ? ComponerCartaConFicha(carta, _fichas.ObtenerFicha(ficha), celda.Width, celda.Height)
            : EscalarImagen(carta, celda.Width, celda.Height);
        celda.Invalidate();
    }

    private void RefrescarTodasLasCeldas()
    {
        foreach (Control c in grilla.Controls)
            if (c is PictureBox celda) RefrescarCelda(celda, (int)celda.Tag!);
    }

    private void ResaltarCarta(int num, Color color)
    {
        foreach (Control c in grilla.Controls)
            if (c is PictureBox celda && (int)celda.Tag! == num)
                celda.BackColor = color;
    }

    // =========================================================================
    // SIGNALR
    // =========================================================================

    private async Task ConectarAsync()
    {
        try
        {
            await _cliente.ConectarAsync();
            await _cliente.UnirseAlJuego(_nombreJugador);
        }
        catch (Exception ex) { MostrarMensaje($"Error al conectar: {ex.Message}", Color.Red); }
    }

    private void SuscribirEventosSignalR()
    {
        _cliente.RolAsignado += (esHost, _) =>
            UI(() => MostrarMensaje($"Conectado como {(esHost ? "Gritón" : "Jugador")}", Color.DimGray));

        _cliente.TablaAsignada += casillas => UI(() =>
        {
            RenderizarTabla(casillas);
            if (!_partidaEnCurso) btnNuevaTabla.Enabled = true;
        });

        _cliente.JuegoIniciado += formato => UI(() =>
        {
            _partidaEnCurso = true;
            btnLoteria.Enabled = true;
            btnNuevaTabla.Enabled = false;
            if (_esHost) { btnCantarCarta.Enabled = true; btnIniciar.Enabled = false; }
            MostrarMensaje($"¡Juego iniciado! Formato: {formato}", Color.FromArgb(0, 104, 56));
        });

        _cliente.JuegoYaIniciado += (formato, casillas) => UI(() =>
        {
            _partidaEnCurso = true;
            btnLoteria.Enabled = true;
            btnNuevaTabla.Enabled = false;
            if (_esHost) { btnCantarCarta.Enabled = true; btnIniciar.Enabled = false; }
            RenderizarTabla(casillas);
            MostrarMensaje($"Te uniste a una partida en curso. Formato: {formato}", Color.DimGray);
        });

        _cliente.CartaCantada += async (num, nombre, frase) =>
        {
            UI(() =>
            {
                _cartasCantadas.Add(num);
                lblNombreCarta.Text = $"{num} — {nombre}";
                lblFrase.Text = $"\"{frase}\"";
                picCarta.Image = _imagenes.ObtenerImagenCarta(num);
                AgregarAlHistorial(num, nombre);
                ResaltarCarta(num, Color.FromArgb(200, 255, 200));
            });

            _cantandoCarta = true;
            try
            {
                await _tts.CantarCartaAsync(frase, nombre);
            }
            finally
            {
                _cantandoCarta = false;
                if (_esHost && _modoAutomatico && _partidaEnCurso)
                    UI(() => _timerAutoCanto.Start());
            }
        };

        _cliente.BarajaRebrajada += () => UI(() =>
            MostrarMensaje("🔀 Baraja agotada — ¡se volvió a barajar!", Color.DarkOrange));

        _cliente.BarajaReiniciada += () => UI(() =>
        {
            LimpiarHistorial();
            _cartasCantadas.Clear();
            _puntos = 0; lblPuntos.Text = "⭐ Puntos: 0";
            MostrarMensaje("Baraja reiniciada.", Color.DimGray);
        });

        _cliente.SalaReiniciada += () => UI(() =>
        {
            _partidaEnCurso = false; _puntos = 0; lblPuntos.Text = "⭐ Puntos: 0";
            btnLoteria.Enabled = false; btnNuevaTabla.Enabled = true;
            if (_esHost) { btnCantarCarta.Enabled = false; btnIniciar.Enabled = true; }
            LimpiarHistorial();
            _cartasCantadas.Clear();
            MostrarMensaje("🔄 Sala reiniciada.", Color.FromArgb(0, 104, 56));
        });

        _cliente.MarcasActualizadas += marcas => UI(() =>
        {
            var vigentes = marcas.ToHashSet();
            foreach (var n in _fichasEnTabla.Keys.Where(k => !vigentes.Contains(k)).ToList())
                _fichasEnTabla.Remove(n);
            RefrescarTodasLasCeldas();
        });

        _cliente.HayGanador += ganador => UI(() =>
        {
            _partidaEnCurso = false;
            btnLoteria.Enabled = false; btnNuevaTabla.Enabled = true;
            if (_esHost) { btnCantarCarta.Enabled = false; btnIniciar.Enabled = true; }
            var msg = ganador == _nombreJugador ? "🏆 ¡GANASTE! ¡LOTERÍA!" : $"🏅 {ganador} ganó.";
            MostrarMensaje(msg, Color.FromArgb(206, 17, 38));
            MessageBox.Show(msg, "¡LOTERÍA!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        _cliente.Trampa += invalidas => UI(() =>
        {
            MostrarMensaje("⚠️ ¡Trampa! Fichas en cartas que no han salido.", Color.Red);
            foreach (Control c in grilla.Controls)
                if (c is PictureBox celda && invalidas.Contains((int)celda.Tag!))
                    celda.BackColor = Color.FromArgb(255, 160, 160);
        });

        _cliente.FalsaAlarma += () => UI(() => MostrarMensaje("Aún no cumples el patrón.", Color.OrangeRed));

        _cliente.JugadoresActualizados += lista => UI(() =>
        {
            lstJugadores.Items.Clear();
            foreach (var j in lista)
                lstJugadores.Items.Add(new JugadorItem(j.Nombre, j.EsGriton, j.Victorias, j.Puntos));
            lblConectados.Text = $"Conectados: {lista.Count}";
        });

        _cliente.PuntosActualizados += pts => UI(() =>
        {
            _puntos = pts; lblPuntos.Text = $"⭐ Puntos: {_puntos}";
        });

        _cliente.FormatoAgregado += formato => UI(() =>
        {
            if (Enum.TryParse<FormatoGanador>(formato, out var formatoAgregado))
                _formatosActivos.Add(formatoAgregado);

            MostrarMensaje($"Formato agregado: {formato}", Color.DarkGreen);
        });

        _cliente.Desconectado += msg => UI(() => MostrarMensaje($"Desconectado: {msg}", Color.Red));
        _cliente.MensajeRecibido += (n, t) => UI(() => AgregarMensajeChat(n, t));
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private void AgregarAlHistorial(int num, string nombre)
    {
        var pic = new PictureBox
        {
            Size = new Size(50, 50),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Margin = new Padding(2),
            Image = _imagenes.ObtenerImagenCarta(num),
        };
        var tip = new ToolTip(); tip.SetToolTip(pic, $"{num} - {nombre}"); _tooltips.Add(tip);
        flpHistorial.Controls.Add(pic);
        flpHistorial.ScrollControlIntoView(pic);
    }

    private void LimpiarHistorial()
    {
        foreach (Control c in flpHistorial.Controls) c.Dispose();
        flpHistorial.Controls.Clear();
        _fichasEnTabla.Clear();
        RefrescarTodasLasCeldas();
        lblNombreCarta.Text = lblFrase.Text = string.Empty;
        picCarta.Image = null;
    }

    private void VerificarRecursosCartas()
    {
        if (_imagenes.ObtenerImagenCarta(1) == null)
            MostrarMensaje($"⚠️ Imágenes no encontradas en: {_imagenes.CarpetaEfectiva}", Color.Red);
    }

    private void MostrarMensaje(string texto, Color color)
    {
        lblMensaje.Text = texto;
        lblMensaje.ForeColor = color;
    }

    private void UI(Action a)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(a); else a();
    }

    private async Task EnviarMensajeChat()
    {
        var texto = txtMensaje?.Text.Trim();
        if (string.IsNullOrEmpty(texto)) return;
        txtMensaje!.Clear();
        try { await _cliente.EnviarMensaje(texto); } catch { }
    }

    private void AgregarMensajeChat(string remitente, string texto)
    {
        bool mio = remitente == _nombreJugador;
        var hora = DateTime.Now.ToString("HH:mm");

        rtbChat.SelectionStart = rtbChat.TextLength;
        rtbChat.SelectionLength = 0;

        using var fNombre = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fHora = new Font("Segoe UI", 7.5f);
        using var fTexto = new Font("Segoe UI", 9.5f);
        using var cNombre = new SolidBrush(mio ? Color.FromArgb(0, 104, 56) : Color.FromArgb(206, 17, 38));
        using var cHora = new SolidBrush(Color.FromArgb(160, 160, 160));
        using var cTexto = new SolidBrush(Color.FromArgb(40, 40, 40));

        rtbChat.SelectionColor = cNombre.Color; rtbChat.SelectionFont = fNombre;
        rtbChat.AppendText($"{remitente}  ");
        rtbChat.SelectionColor = cHora.Color; rtbChat.SelectionFont = fHora;
        rtbChat.AppendText($"{hora}\n");
        rtbChat.SelectionColor = cTexto.Color; rtbChat.SelectionFont = fTexto;
        rtbChat.AppendText($"{texto}\n\n");
        rtbChat.ScrollToCaret();
    }

    private void lstJugadores_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstJugadores.Items.Count) return;
        if (lstJugadores.Items[e.Index] is not JugadorItem item) return;

        e.DrawBackground();
        using var bg = new SolidBrush(e.Index % 2 == 0 ? Color.White : Color.FromArgb(250, 248, 240));
        e.Graphics.FillRectangle(bg, e.Bounds);

        using var fN = new Font("Segoe UI", 9.5f, item.EsGriton ? FontStyle.Bold : FontStyle.Regular);
        using var fS = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var cN = new SolidBrush(item.EsGriton ? Color.FromArgb(0, 104, 56) : Color.FromArgb(40, 40, 40));
        using var cV = new SolidBrush(Color.FromArgb(180, 20, 35));
        using var cP = new SolidBrush(Color.FromArgb(120, 75, 0));

        var r = e.Bounds;
        e.Graphics.DrawString(item.Nombre, fN, cN,
            new RectangleF(r.X + 6, r.Y + 3, r.Width - 12, 18));

        var statsY = r.Y + 22;
        if (item.Victorias > 0)
            e.Graphics.DrawString($"V: {item.Victorias}", fS, cV,
                new RectangleF(r.X + 8, statsY, 58, 18));
        if (item.Puntos > 0)
            e.Graphics.DrawString($"Pts: {item.Puntos}", fS, cP,
                new RectangleF(r.X + 70, statsY, r.Width - 76, 18));

        using var sep = new Pen(Color.FromArgb(230, 230, 220));
        e.Graphics.DrawLine(sep, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
        e.DrawFocusRectangle();
    }

    // ── Imágenes ──────────────────────────────────────────────────────────────

    private static Image EscalarImagen(Image? src, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        AltaCalidad(g); g.Clear(Color.White);
        if (src != null) g.DrawImage(src, 0, 0, w, h);
        return bmp;
    }

    private static Image ComponerCartaConFicha(Image? carta, Image? ficha, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        AltaCalidad(g); g.Clear(Color.White);
        if (carta != null) g.DrawImage(carta, 0, 0, w, h);
        if (ficha != null)
        {
            int tam = (int)(Math.Min(w, h) * 0.70);
            g.DrawImage(ficha, (w - tam) / 2, (h - tam) / 2, tam, tam);
        }
        return bmp;
    }

    private static void AltaCalidad(Graphics g)
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
    }



    private void cmbVelocidad_SelectedIndexChanged(object sender, EventArgs e)
    {
        switch (cmbVelocidad.Text)
        {
            case "Lento":
                _tts.CambiarVelocidad(-3);
                _timerAutoCanto.Interval = 3000;
                break;

            case "Normal":
                _tts.CambiarVelocidad(0);
                _timerAutoCanto.Interval = 1500;
                break;

            case "Rápido":
                _tts.CambiarVelocidad(3);
                _timerAutoCanto.Interval = 500;
                break;
        }

        if (_modoAutomatico && !_cantandoCarta && _partidaEnCurso)
        {
            _timerAutoCanto.Stop();
            _timerAutoCanto.Start();
        }
    }
    private void btnAuto_Click_1(object sender, EventArgs e)
    {
        _modoAutomatico = true;
        _timerAutoCanto.Start();
    }

    private void btnDetenerAuto_Click_1(object sender, EventArgs e)
    {
        _modoAutomatico = false;
        _timerAutoCanto.Stop();
    }

    private async void btnAgregarFormato_Click(object sender, EventArgs e)
    {
        var formato = (FormatoGanador)cmbFormatoExtra.SelectedItem!;

        if (_formatosActivos.Contains(formato))
        {
            MostrarMensaje(
                $"El formato {formato} ya estaba activo.",
                Color.Orange);
            return;
        }

        try
        {
            await _cliente.AgregarFormato(formato.ToString());
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo agregar formato: {ex.Message}", Color.Red);
        }
    }

    // ── Tipos auxiliares ──────────────────────────────────────────────────────

    private record TablaGuardadaDto(string NombreJugador, string NombreTabla,
        string FechaGuardado, List<TablaJsonDto> Casillas);

    private record JugadorItem(string Nombre, bool EsGriton, int Victorias, int Puntos = 0)
    {
        public override string ToString() => Nombre;
    }
}
