using LoteriaMexicana.Domain;
using LoteriaMexicana.Hubs;

namespace LoteriaMexicana.Forms;

public class FormCrearTabla : Form
{
    private readonly CheckedListBox lstCartas = new();
    private readonly ComboBox cmbCartaRepetida = new();
    private readonly RadioButton rb4x4 = new();
    private readonly RadioButton rb5x5 = new();
    private readonly RadioButton rbSimple = new();
    private readonly RadioButton rbDoble = new();
    private readonly Label lblSeleccion = new();
    private readonly Button btnCrear = new();
    private readonly List<Carta> cartasDisponibles;

    public TablaJugadorDto? TablaCreada { get; private set; }
    public int FilasSeleccionadas => rb4x4.Checked ? 4 : 5;
    public int ColumnasSeleccionadas => FilasSeleccionadas;
    public bool TablaDoble => rbDoble.Checked;

    private int TotalCasillas => FilasSeleccionadas * ColumnasSeleccionadas;
    private int CartasUnicasNecesarias => TablaDoble ? TotalCasillas - 1 : TotalCasillas;

    public FormCrearTabla(IEnumerable<Carta> cartas)
    {
        cartasDisponibles = cartas.OrderBy(c => c.Numero).ToList();
        InitializeComponent();
        CargarCartas();
        ActualizarEstado();
    }

    private void InitializeComponent()
    {
        Text = "Crear tabla";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 620);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);

        var lblTamano = CrearEtiqueta("Tamano:", 20, 18);
        var pnlTamano = new Panel { Location = new Point(20, 43), Size = new Size(210, 32), BackColor = Color.Transparent };
        rb4x4.Text = "4 x 4";
        rb4x4.Location = new Point(0, 2);
        rb4x4.AutoSize = true;
        rb5x5.Text = "5 x 5";
        rb5x5.Location = new Point(95, 2);
        rb5x5.AutoSize = true;
        rb5x5.Checked = true;
        rb4x4.CheckedChanged += (_, _) => ReiniciarSeleccion();
        rb5x5.CheckedChanged += (_, _) => ReiniciarSeleccion();
        pnlTamano.Controls.AddRange(new Control[] { rb4x4, rb5x5 });

        var lblTipo = CrearEtiqueta("Tipo:", 270, 18);
        var pnlTipo = new Panel { Location = new Point(270, 43), Size = new Size(220, 32), BackColor = Color.Transparent };
        rbSimple.Text = "Simple";
        rbSimple.Location = new Point(0, 2);
        rbSimple.AutoSize = true;
        rbSimple.Checked = true;
        rbDoble.Text = "Doble";
        rbDoble.Location = new Point(100, 2);
        rbDoble.AutoSize = true;
        rbSimple.CheckedChanged += (_, _) => ReiniciarSeleccion();
        rbDoble.CheckedChanged += (_, _) => ReiniciarSeleccion();
        pnlTipo.Controls.AddRange(new Control[] { rbSimple, rbDoble });

        lblSeleccion.Location = new Point(20, 88);
        lblSeleccion.Size = new Size(470, 28);
        lblSeleccion.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblSeleccion.ForeColor = Color.FromArgb(0, 104, 56);

        lstCartas.Location = new Point(20, 122);
        lstCartas.Size = new Size(470, 360);
        lstCartas.CheckOnClick = true;
        lstCartas.ItemCheck += lstCartas_ItemCheck;

        var lblRepetida = CrearEtiqueta("Carta repetida:", 20, 495);
        cmbCartaRepetida.Location = new Point(140, 492);
        cmbCartaRepetida.Size = new Size(350, 28);
        cmbCartaRepetida.DropDownStyle = ComboBoxStyle.DropDownList;

        btnCrear.Text = "Crear tabla";
        btnCrear.Location = new Point(342, 550);
        btnCrear.Size = new Size(148, 38);
        btnCrear.BackColor = Color.FromArgb(0, 104, 56);
        btnCrear.ForeColor = Color.White;
        btnCrear.FlatStyle = FlatStyle.Flat;
        btnCrear.FlatAppearance.BorderSize = 0;
        btnCrear.Cursor = Cursors.Hand;
        btnCrear.Click += btnCrear_Click;

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            Location = new Point(240, 550),
            Size = new Size(92, 38),
            DialogResult = DialogResult.Cancel,
            BackColor = Color.FromArgb(235, 235, 235),
            ForeColor = Color.FromArgb(60, 60, 60),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
        };
        btnCancelar.FlatAppearance.BorderSize = 0;

        AcceptButton = btnCrear;
        CancelButton = btnCancelar;
        Controls.AddRange(new Control[]
        {
            lblTamano, pnlTamano,
            lblTipo, pnlTipo,
            lblSeleccion, lstCartas,
            lblRepetida, cmbCartaRepetida,
            btnCrear, btnCancelar,
        });
    }

    private static Label CrearEtiqueta(string texto, int x, int y) => new()
    {
        AutoSize = true,
        Text = texto,
        Location = new Point(x, y),
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
    };

    private void CargarCartas()
    {
        lstCartas.Items.Clear();
        foreach (var carta in cartasDisponibles)
            lstCartas.Items.Add(carta);
    }

    private void ReiniciarSeleccion()
    {
        if (!IsHandleCreated) return;
        for (int i = 0; i < lstCartas.Items.Count; i++)
            lstCartas.SetItemChecked(i, false);
        ActualizarEstado();
    }

    private void lstCartas_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        int seleccionadas = lstCartas.CheckedItems.Count;
        if (e.NewValue == CheckState.Checked && e.CurrentValue != CheckState.Checked)
            seleccionadas++;
        else if (e.NewValue != CheckState.Checked && e.CurrentValue == CheckState.Checked)
            seleccionadas--;

        if (seleccionadas > CartasUnicasNecesarias)
        {
            e.NewValue = CheckState.Unchecked;
            MessageBox.Show($"Solo puedes elegir {CartasUnicasNecesarias} cartas para esta tabla.",
                "Limite de cartas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        BeginInvoke(ActualizarEstado);
    }

    private void ActualizarEstado()
    {
        var seleccionadas = ObtenerCartasSeleccionadas();
        lblSeleccion.Text = $"Seleccionadas: {seleccionadas.Count} de {CartasUnicasNecesarias}";

        cmbCartaRepetida.Enabled = TablaDoble;
        cmbCartaRepetida.DataSource = null;
        if (TablaDoble)
        {
            cmbCartaRepetida.DataSource = seleccionadas.ToList();
            btnCrear.Enabled = seleccionadas.Count == CartasUnicasNecesarias && cmbCartaRepetida.Items.Count > 0;
        }
        else
        {
            btnCrear.Enabled = seleccionadas.Count == CartasUnicasNecesarias;
        }
    }

    private List<Carta> ObtenerCartasSeleccionadas()
        => lstCartas.CheckedItems.Cast<Carta>().ToList();

    private void btnCrear_Click(object? sender, EventArgs e)
    {
        var cartas = ObtenerCartasSeleccionadas();
        if (cartas.Count != CartasUnicasNecesarias)
        {
            MessageBox.Show($"Selecciona {CartasUnicasNecesarias} cartas.",
                "Tabla incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (TablaDoble)
        {
            if (cmbCartaRepetida.SelectedItem is not Carta repetida)
            {
                MessageBox.Show("Selecciona la carta que se repetira.",
                    "Tabla doble", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            cartas.Add(repetida);
        }

        TablaCreada = new TablaJugadorDto(
            0,
            cartas.Select(c => new CasillaDto(c.Numero, c.Nombre)).ToList());
        DialogResult = DialogResult.OK;
    }
}