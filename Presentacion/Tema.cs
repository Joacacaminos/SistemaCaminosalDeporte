using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SistemaCaminosalDeporte.Presentacion
{
    /// <summary>Colores, fuentes y controles base con el estilo oscuro/verde del mockup.</summary>
    public static class Tema
    {
        public static readonly Color Fondo = Color.FromArgb(11, 21, 18);
        public static readonly Color Panel = Color.FromArgb(16, 28, 24);
        public static readonly Color Tarjeta = Color.FromArgb(20, 36, 31);
        public static readonly Color Borde = Color.FromArgb(30, 48, 41);
        public static readonly Color Verde = Color.FromArgb(34, 197, 94);
        public static readonly Color Texto = Color.FromArgb(230, 242, 236);
        public static readonly Color Gris = Color.FromArgb(138, 163, 154);
        public static readonly Color Rojo = Color.FromArgb(239, 68, 68);

        public static readonly CultureInfo Ar = new CultureInfo("es-AR");

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        public static string Dinero(decimal valor)
        {
            return "$ " + valor.ToString("N0", Ar);
        }

        public static Font Fuente(float tam, bool negrita = false)
        {
            return new Font("Segoe UI", tam, negrita ? FontStyle.Bold : FontStyle.Regular);
        }

        public static Label Etiqueta(string texto, float tam = 10, bool negrita = false, Color? color = null)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = Fuente(tam, negrita),
                ForeColor = color ?? Texto,
                BackColor = Color.Transparent
            };
        }

        public static Button Boton(string texto, bool primario = true)
        {
            var b = new Button
            {
                Text = texto,
                FlatStyle = FlatStyle.Flat,
                Height = 38,
                Cursor = Cursors.Hand,
                Font = Fuente(10, true)
            };
            b.FlatAppearance.BorderSize = primario ? 0 : 1;
            b.FlatAppearance.BorderColor = Borde;
            Marcar(b, primario);
            return b;
        }

        /// <summary>Pinta un botón como seleccionado (verde) o normal.</summary>
        public static void Marcar(Button b, bool activo)
        {
            b.BackColor = activo ? Verde : Tarjeta;
            b.ForeColor = activo ? Color.Black : Texto;
        }

        public static TextBox Caja(string placeholder)
        {
            var t = new TextBox
            {
                BackColor = Tarjeta,
                ForeColor = Texto,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Fuente(11)
            };
            // Texto de ayuda (placeholder) en .NET Framework
            t.HandleCreated += (s, e) => SendMessage(t.Handle, 0x1501, 1, placeholder);
            return t;
        }

        public static Panel NuevaTarjeta()
        {
            var p = new Panel { BackColor = Tarjeta };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Borde))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            p.Resize += (s, e) => p.Invalidate();
            return p;
        }

        public static DataGridView NuevaGrilla()
        {
            var g = new DataGridView
            {
                EnableHeadersVisualStyles = false,
                BackgroundColor = Tarjeta,
                BorderStyle = BorderStyle.None,
                GridColor = Borde,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                Font = Fuente(9.5f)
            };
            g.RowTemplate.Height = 34;
            g.ColumnHeadersDefaultCellStyle.BackColor = Panel;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Gris;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Panel;
            g.DefaultCellStyle.BackColor = Tarjeta;
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(24, 70, 46);
            g.DefaultCellStyle.SelectionForeColor = Texto;
            return g;
        }
    }
}
