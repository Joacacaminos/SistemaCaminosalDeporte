using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaCaminosalDeporte.Presentacion
{
    public class FrmLogin : Form
    {
        private TextBox txtUsuario;
        private TextBox txtClave;
        private ComboBox cmbRol;
        private Label lblError;

        public string UsuarioIngresado { get { return txtUsuario.Text.Trim(); } }
        public string RolSeleccionado { get { return (string)cmbRol.SelectedItem; } }

        public FrmLogin()
        {
            Text = "Caminos Ropa Deportiva - Ingreso";
            ClientSize = new Size(400, 480);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Tema.Fondo;

            Controls.Add(Centrado("CAMINOS", 26, true, Tema.Verde, 40, 44));
            Controls.Add(Centrado("ROPA DEPORTIVA", 10, false, Tema.Gris, 88, 22));
            Controls.Add(Centrado("Ingresá para continuar", 11, false, Tema.Texto, 140, 26));

            Controls.Add(Etq("Usuario", 50, 186));
            txtUsuario = Tema.Caja("Usuario");
            txtUsuario.SetBounds(50, 208, 300, 30);
            Controls.Add(txtUsuario);

            Controls.Add(Etq("Contraseña", 50, 252));
            txtClave = Tema.Caja("Contraseña");
            txtClave.UseSystemPasswordChar = true;
            txtClave.SetBounds(50, 274, 300, 30);
            Controls.Add(txtClave);

            // Solo para la demo visual: cuando exista la base de datos, el rol sale del usuario.
            Controls.Add(Etq("Rol (solo demo)", 50, 318));
            cmbRol = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.Tarjeta,
                ForeColor = Tema.Texto,
                Font = Tema.Fuente(11)
            };
            cmbRol.Items.AddRange(new object[] { "Dueño", "Empleado", "Administrador" });
            cmbRol.SelectedIndex = 0;
            cmbRol.SetBounds(50, 340, 300, 30);
            Controls.Add(cmbRol);

            lblError = Tema.Etiqueta("", 9, false, Tema.Rojo);
            lblError.Location = new Point(50, 378);
            lblError.Visible = false;
            Controls.Add(lblError);

            var btnIngresar = Tema.Boton("Ingresar");
            btnIngresar.SetBounds(50, 402, 300, 44);
            btnIngresar.Click += Ingresar;
            Controls.Add(btnIngresar);
            AcceptButton = btnIngresar;
        }

        private void Ingresar(object sender, EventArgs e)
        {
            if (txtUsuario.Text.Trim().Length == 0 || txtClave.Text.Length == 0)
            {
                lblError.Text = "Completá usuario y contraseña.";
                lblError.Visible = true;
                return;
            }
            // TODO (capa Negocio): validar usuario y contraseña (hash) contra la tabla Usuario.
            DialogResult = DialogResult.OK;
        }

        private Label Etq(string texto, int x, int y)
        {
            var l = Tema.Etiqueta(texto, 9, false, Tema.Gris);
            l.Location = new Point(x, y);
            return l;
        }

        private Label Centrado(string texto, float tam, bool negrita, Color color, int y, int alto)
        {
            return new Label
            {
                Text = texto,
                AutoSize = false,
                Size = new Size(400, alto),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Tema.Fuente(tam, negrita),
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }
    }
}
