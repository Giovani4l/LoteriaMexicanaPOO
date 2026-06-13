using LoteriaMexicana.Domain.Enums;

namespace LoteriaMexicana.Forms;

public partial class FormConfigurarPartida : Form
{
    public FormatoGanador FormatoSeleccionado { get; private set; }
    public int Filas { get; private set; }
    public int Columnas { get; private set; }
    public bool TablaDoble { get; private set; }

    private static readonly (string Etiqueta, FormatoGanador Valor)[] Formatos =
    {
        ("Línea Horizontal", FormatoGanador.LineaHorizontal),
        ("Línea Vertical",   FormatoGanador.LineaVertical),
        ("Diagonal",         FormatoGanador.Diagonal),
        ("Cruz",             FormatoGanador.Cruz),
        ("Cruzita",          FormatoGanador.Cruzita),
        ("Tabla Llena",      FormatoGanador.TablaLlena),
    };

    public FormConfigurarPartida(FormatoGanador formatoActual, int filasActual, bool tablaDobleActual)
    {
        InitializeComponent();

        foreach (var (etiqueta, valor) in Formatos)
            cmbFormato.Items.Add(new FormatoItem(etiqueta, valor));

        int idx = Array.FindIndex(Formatos, f => f.Valor == formatoActual);
        cmbFormato.SelectedIndex = idx >= 0 ? idx : 0;

        rb4x4.Checked = filasActual == 4;
        rb5x5.Checked = filasActual == 5;
        rbSimple.Checked = !tablaDobleActual;
        rbDoble.Checked = tablaDobleActual;
    }

    private void btnConfirmar_Click(object sender, EventArgs e)
    {
        FormatoSeleccionado = ((FormatoItem)cmbFormato.SelectedItem!).Valor;
        Filas = rb4x4.Checked ? 4 : 5;
        Columnas = rb4x4.Checked ? 4 : 5;
        TablaDoble = rbDoble.Checked;
        DialogResult = DialogResult.OK;
    }

    private record FormatoItem(string Etiqueta, FormatoGanador Valor)
    {
        public override string ToString() => Etiqueta;
    }
}
