namespace LoteriaMexicana.Forms;

partial class FormConfigurarPartida
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        cmbFormato = new ComboBox();
        rb4x4 = new RadioButton();
        rb5x5 = new RadioButton();
        nudTablas = new NumericUpDown();
        rbSimple = new RadioButton();
        rbDoble = new RadioButton();
        btnConfirmar = new Button();
        btnCancelar = new Button();

        var lblFormato = new Label();
        var lblTamano = new Label();
        var lblTablas = new Label();
        var lblTipo = new Label();
        var pnlTamano = new Panel();
        var pnlTipo = new Panel();

        SuspendLayout();
        pnlTamano.SuspendLayout();
        pnlTipo.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudTablas).BeginInit();

        lblFormato.AutoSize = true;
        lblFormato.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        lblFormato.Location = new Point(20, 20);
        lblFormato.Text = "Formato ganador:";

        cmbFormato.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFormato.Font = new Font("Segoe UI", 9.5f);
        cmbFormato.Location = new Point(20, 45);
        cmbFormato.Size = new Size(345, 28);

        lblTamano.AutoSize = true;
        lblTamano.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        lblTamano.Location = new Point(20, 95);
        lblTamano.Text = "Tamano de tabla:";

        pnlTamano.Location = new Point(20, 118);
        pnlTamano.Size = new Size(345, 58);
        pnlTamano.BackColor = Color.Transparent;

        rb4x4.AutoSize = true;
        rb4x4.Font = new Font("Segoe UI", 9.5f);
        rb4x4.Location = new Point(0, 0);
        rb4x4.Text = "4 x 4  (16 cartas)";

        rb5x5.AutoSize = true;
        rb5x5.Font = new Font("Segoe UI", 9.5f);
        rb5x5.Location = new Point(0, 28);
        rb5x5.Text = "5 x 5  (25 cartas)";

        pnlTamano.Controls.Add(rb4x4);
        pnlTamano.Controls.Add(rb5x5);

        lblTablas.AutoSize = true;
        lblTablas.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        lblTablas.Location = new Point(20, 190);
        lblTablas.Text = "Tablas:";

        nudTablas.Font = new Font("Segoe UI", 9.5f);
        nudTablas.Location = new Point(90, 187);
        nudTablas.Minimum = 1;
        nudTablas.Maximum = 99;
        nudTablas.Value = 1;
        nudTablas.Size = new Size(90, 28);

        lblTipo.AutoSize = true;
        lblTipo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        lblTipo.Location = new Point(20, 232);
        lblTipo.Text = "Tipo de tabla:";

        pnlTipo.Location = new Point(20, 255);
        pnlTipo.Size = new Size(345, 58);
        pnlTipo.BackColor = Color.Transparent;

        rbSimple.AutoSize = true;
        rbSimple.Font = new Font("Segoe UI", 9.5f);
        rbSimple.Location = new Point(0, 0);
        rbSimple.Text = "Simple  (todas las cartas son unicas)";

        rbDoble.AutoSize = true;
        rbDoble.Font = new Font("Segoe UI", 9.5f);
        rbDoble.Location = new Point(0, 28);
        rbDoble.Text = "Doble  (una carta se repite dos veces)";

        pnlTipo.Controls.Add(rbSimple);
        pnlTipo.Controls.Add(rbDoble);

        btnConfirmar.BackColor = Color.FromArgb(0, 104, 56);
        btnConfirmar.Cursor = Cursors.Hand;
        btnConfirmar.FlatAppearance.BorderSize = 0;
        btnConfirmar.FlatStyle = FlatStyle.Flat;
        btnConfirmar.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        btnConfirmar.ForeColor = Color.White;
        btnConfirmar.Location = new Point(216, 337);
        btnConfirmar.Size = new Size(148, 38);
        btnConfirmar.Text = "Iniciar partida";
        btnConfirmar.Click += btnConfirmar_Click;

        btnCancelar.BackColor = Color.FromArgb(235, 235, 235);
        btnCancelar.Cursor = Cursors.Hand;
        btnCancelar.DialogResult = DialogResult.Cancel;
        btnCancelar.FlatAppearance.BorderSize = 0;
        btnCancelar.FlatStyle = FlatStyle.Flat;
        btnCancelar.Font = new Font("Segoe UI", 9f);
        btnCancelar.ForeColor = Color.FromArgb(60, 60, 60);
        btnCancelar.Location = new Point(120, 337);
        btnCancelar.Size = new Size(88, 38);
        btnCancelar.Text = "Cancelar";

        AcceptButton = btnConfirmar;
        CancelButton = btnCancelar;
        BackColor = Color.White;
        ClientSize = new Size(400, 395);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Configurar partida";

        Controls.AddRange(new Control[]
        {
            lblFormato, cmbFormato,
            lblTamano, pnlTamano,
            lblTablas, nudTablas,
            lblTipo, pnlTipo,
            btnConfirmar, btnCancelar,
        });

        ((System.ComponentModel.ISupportInitialize)nudTablas).EndInit();
        pnlTamano.ResumeLayout(false);
        pnlTamano.PerformLayout();
        pnlTipo.ResumeLayout(false);
        pnlTipo.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private ComboBox cmbFormato;
    private RadioButton rb4x4, rb5x5, rbSimple, rbDoble;
    private NumericUpDown nudTablas;
    private Button btnConfirmar, btnCancelar;
}
