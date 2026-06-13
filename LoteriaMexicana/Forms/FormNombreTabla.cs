namespace LoteriaMexicana.Forms;

public partial class FormNombreTabla : Form
{
    public string NombreIngresado => txtNombre.Text.Trim();

    public FormNombreTabla()
    {
        InitializeComponent();
    }

    private void btnGuardar_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text)) return;
        DialogResult = DialogResult.OK;
    }
}
