namespace LoteriaMexicana.Forms;

partial class FormNombreTabla
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        txtNombre = new TextBox();
        btnGuardar = new Button();
        lblNombre = new Label();
        SuspendLayout();
        // 
        // txtNombre
        // 
        txtNombre.Font = new Font("Segoe UI", 10F);
        txtNombre.Location = new Point(16, 46);
        txtNombre.MaxLength = 50;
        txtNombre.Name = "txtNombre";
        txtNombre.Size = new Size(294, 30);
        txtNombre.TabIndex = 1;
        // 
        // btnGuardar
        // 
        btnGuardar.BackColor = Color.FromArgb(0, 104, 56);
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.FlatStyle = FlatStyle.Flat;
        btnGuardar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnGuardar.ForeColor = Color.White;
        btnGuardar.Location = new Point(214, 82);
        btnGuardar.Name = "btnGuardar";
        btnGuardar.Size = new Size(96, 32);
        btnGuardar.TabIndex = 2;
        btnGuardar.Text = "Guardar";
        btnGuardar.UseVisualStyleBackColor = false;
        btnGuardar.Click += btnGuardar_Click;
        // 
        // lblNombre
        // 
        lblNombre.AutoSize = true;
        lblNombre.Font = new Font("Segoe UI", 9F);
        lblNombre.Location = new Point(16, 20);
        lblNombre.Name = "lblNombre";
        lblNombre.Size = new Size(225, 20);
        lblNombre.TabIndex = 0;
        lblNombre.Text = "Ingresa un nombre para tu tabla:";
        // 
        // FormNombreTabla
        // 
        AcceptButton = btnGuardar;
        ClientSize = new Size(376, 165);
        Controls.Add(lblNombre);
        Controls.Add(txtNombre);
        Controls.Add(btnGuardar);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FormNombreTabla";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Nombre de la tabla";
        ResumeLayout(false);
        PerformLayout();
    }

    private TextBox txtNombre;
    private Button  btnGuardar;
    private Label lblNombre;
}
