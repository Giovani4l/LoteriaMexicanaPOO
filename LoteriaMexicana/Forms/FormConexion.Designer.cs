namespace LoteriaMexicana.Forms;

partial class FormConexion
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Código generado por el Diseñador de Windows Forms

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConexion));
        pnlHeader = new Panel();
        lblSubtitulo = new Label();
        lblTitulo = new Label();
        pnlBandaDorada = new Panel();
        pnlContenido = new Panel();
        lblNombreTxt = new Label();
        txtNombre = new TextBox();
        btnCrear = new Button();
        lblIpTxt = new Label();
        txtIp = new TextBox();
        btnUnirse = new Button();
        pnlHeader.SuspendLayout();
        pnlContenido.SuspendLayout();
        SuspendLayout();
        // 
        // pnlHeader
        // 
        pnlHeader.BackColor = Color.Transparent;
        pnlHeader.BackgroundImage = (Image)resources.GetObject("pnlHeader.BackgroundImage");
        pnlHeader.BackgroundImageLayout = ImageLayout.Stretch;
        pnlHeader.Controls.Add(lblSubtitulo);
        pnlHeader.Controls.Add(lblTitulo);
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Location = new Point(0, 0);
        pnlHeader.Name = "pnlHeader";
        pnlHeader.Size = new Size(480, 110);
        pnlHeader.TabIndex = 2;
        // 
        // lblSubtitulo
        // 
        lblSubtitulo.BackColor = Color.Transparent;
        lblSubtitulo.Font = new Font("Segoe UI", 13F, FontStyle.Italic);
        lblSubtitulo.ForeColor = Color.FromArgb(255, 220, 180);
        lblSubtitulo.Location = new Point(3, 79);
        lblSubtitulo.Name = "lblSubtitulo";
        lblSubtitulo.Size = new Size(480, 28);
        lblSubtitulo.TabIndex = 1;
        lblSubtitulo.Text = "Mexicana";
        lblSubtitulo.TextAlign = ContentAlignment.TopCenter;
        lblSubtitulo.Click += lblSubtitulo_Click;
        // 
        // lblTitulo
        // 
        lblTitulo.BackColor = Color.Transparent;
        lblTitulo.Font = new Font("Georgia", 32F, FontStyle.Bold | FontStyle.Italic);
        lblTitulo.ForeColor = Color.FromArgb(240, 185, 11);
        lblTitulo.Location = new Point(3, -9);
        lblTitulo.Name = "lblTitulo";
        lblTitulo.Size = new Size(480, 107);
        lblTitulo.TabIndex = 0;
        lblTitulo.Text = "¡LOTERÍA!";
        lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pnlBandaDorada
        // 
        pnlBandaDorada.BackColor = Color.FromArgb(240, 185, 11);
        pnlBandaDorada.Dock = DockStyle.Top;
        pnlBandaDorada.Location = new Point(0, 110);
        pnlBandaDorada.Name = "pnlBandaDorada";
        pnlBandaDorada.Size = new Size(480, 6);
        pnlBandaDorada.TabIndex = 1;
        // 
        // pnlContenido
        // 
        pnlContenido.BackColor = Color.Transparent;
        pnlContenido.BackgroundImage = (Image)resources.GetObject("pnlContenido.BackgroundImage");
        pnlContenido.BackgroundImageLayout = ImageLayout.Stretch;
        pnlContenido.Controls.Add(lblNombreTxt);
        pnlContenido.Controls.Add(txtNombre);
        pnlContenido.Controls.Add(btnCrear);
        pnlContenido.Controls.Add(lblIpTxt);
        pnlContenido.Controls.Add(txtIp);
        pnlContenido.Controls.Add(btnUnirse);
        pnlContenido.Dock = DockStyle.Fill;
        pnlContenido.Location = new Point(0, 116);
        pnlContenido.Name = "pnlContenido";
        pnlContenido.Size = new Size(480, 444);
        pnlContenido.TabIndex = 0;
        // 
        // lblNombreTxt
        // 
        lblNombreTxt.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblNombreTxt.ForeColor = Color.FromArgb(60, 60, 60);
        lblNombreTxt.Location = new Point(36, 20);
        lblNombreTxt.Name = "lblNombreTxt";
        lblNombreTxt.Size = new Size(380, 22);
        lblNombreTxt.TabIndex = 0;
        lblNombreTxt.Text = "Tu nombre:";
        // 
        // txtNombre
        // 
        txtNombre.BackColor = Color.PeachPuff;
        txtNombre.BorderStyle = BorderStyle.FixedSingle;
        txtNombre.Font = new Font("Segoe UI", 11F);
        txtNombre.Location = new Point(36, 46);
        txtNombre.Name = "txtNombre";
        txtNombre.PlaceholderText = "Ej: Juan";
        txtNombre.Size = new Size(380, 32);
        txtNombre.TabIndex = 0;
        // 
        // btnCrear
        // 
        btnCrear.BackColor = Color.Transparent;
        btnCrear.BackgroundImage = (Image)resources.GetObject("btnCrear.BackgroundImage");
        btnCrear.BackgroundImageLayout = ImageLayout.Stretch;
        btnCrear.Cursor = Cursors.Hand;
        btnCrear.FlatAppearance.BorderSize = 0;
        btnCrear.FlatStyle = FlatStyle.Flat;
        btnCrear.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnCrear.ForeColor = Color.White;
        btnCrear.Location = new Point(36, 96);
        btnCrear.Name = "btnCrear";
        btnCrear.Size = new Size(380, 48);
        btnCrear.TabIndex = 1;
        btnCrear.Text = "Crear sala ";
        btnCrear.UseVisualStyleBackColor = false;
        btnCrear.Click += btnCrear_Click;
        // 
        // lblIpTxt
        // 
        lblIpTxt.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblIpTxt.ForeColor = Color.FromArgb(60, 60, 60);
        lblIpTxt.Location = new Point(36, 188);
        lblIpTxt.Name = "lblIpTxt";
        lblIpTxt.Size = new Size(380, 22);
        lblIpTxt.TabIndex = 3;
        lblIpTxt.Text = "IP del Gritón:";
        // 
        // txtIp
        // 
        txtIp.BackColor = Color.PeachPuff;
        txtIp.BorderStyle = BorderStyle.FixedSingle;
        txtIp.Font = new Font("Segoe UI", 11F);
        txtIp.Location = new Point(36, 214);
        txtIp.Name = "txtIp";
        txtIp.PlaceholderText = "Ej: 192.168.1.10";
        txtIp.Size = new Size(380, 32);
        txtIp.TabIndex = 2;
        // 
        // btnUnirse
        // 
        btnUnirse.BackColor = Color.Transparent;
        btnUnirse.BackgroundImage = (Image)resources.GetObject("btnUnirse.BackgroundImage");
        btnUnirse.BackgroundImageLayout = ImageLayout.Stretch;
        btnUnirse.Cursor = Cursors.Hand;
        btnUnirse.FlatAppearance.BorderSize = 0;
        btnUnirse.FlatStyle = FlatStyle.Flat;
        btnUnirse.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnUnirse.ForeColor = Color.White;
        btnUnirse.Location = new Point(36, 264);
        btnUnirse.Name = "btnUnirse";
        btnUnirse.Size = new Size(380, 48);
        btnUnirse.TabIndex = 3;
        btnUnirse.Text = "Unirme a la sala";
        btnUnirse.UseVisualStyleBackColor = false;
        btnUnirse.Click += btnUnirse_Click;
        // 
        // FormConexion
        // 
        AutoScaleDimensions = new SizeF(9F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(0, 104, 56);
        ClientSize = new Size(480, 560);
        Controls.Add(pnlContenido);
        Controls.Add(pnlBandaDorada);
        Controls.Add(pnlHeader);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(480, 560);
        Name = "FormConexion";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Lotería Mexicana";
        pnlHeader.ResumeLayout(false);
        pnlContenido.ResumeLayout(false);
        pnlContenido.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel   pnlHeader;
    private System.Windows.Forms.Panel   pnlBandaDorada;
    private System.Windows.Forms.Label   lblTitulo;
    private System.Windows.Forms.Label   lblSubtitulo;
    private System.Windows.Forms.Panel   pnlContenido;
    private System.Windows.Forms.Label   lblNombreTxt;
    private System.Windows.Forms.TextBox txtNombre;
    private System.Windows.Forms.Button  btnCrear;
    private System.Windows.Forms.Label   lblIpTxt;
    private System.Windows.Forms.TextBox txtIp;
    private System.Windows.Forms.Button  btnUnirse;
}
