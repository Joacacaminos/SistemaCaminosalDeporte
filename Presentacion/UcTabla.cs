using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaCaminosalDeporte.Presentacion
{
    /// <summary>
    /// Vista reutilizable: tarjetas de resumen (opcional) + barra de botones + tabla.
    /// La usan Inicio, Productos, Stock, Reportes y Usuarios.
    /// </summary>
    public class UcTabla : UserControl
    {
        public DataGridView dgvTabla { get; private set; }

        public UcTabla(string[][] kpis, string[] botones, string[] columnas, string[][] filas)
        {
            BackColor = Tema.Fondo;

            // El control "Fill" se agrega primero y los "Top" después (así WinForms acomoda bien el docking).
            dgvTabla = Tema.NuevaGrilla();
            dgvTabla.Dock = DockStyle.Fill;
            foreach (var c in columnas) dgvTabla.Columns.Add(c, c);
            foreach (var f in filas) dgvTabla.Rows.Add(f);
            dgvTabla.CellFormatting += ColorearEstado;
            Controls.Add(dgvTabla);

            if (botones != null && botones.Length > 0)
            {
                var pnlBarra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
                for (int i = 0; i < botones.Length; i++)
                {
                    var btnTabla = Tema.Boton(botones[i], i == 0);
                    btnTabla.AutoSize = true;
                    btnTabla.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    btnTabla.Padding = new Padding(10, 0, 10, 0);
                    btnTabla.Margin = new Padding(0, 0, 8, 0);
                    pnlBarra.Controls.Add(btnTabla);
                }
                Controls.Add(pnlBarra);
            }

            if (kpis != null && kpis.Length > 0)
            {
                var pnlTarjetas = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 104,
                    BackColor = Color.Transparent
                };
                foreach (var k in kpis)
                {
                    var pnlTarjeta = Tema.NuevaTarjeta();
                    pnlTarjeta.Size = new Size(250, 84);
                    pnlTarjeta.Margin = new Padding(0, 0, 12, 12);
                    var lblTitulo = Tema.Etiqueta(k[0], 9.5f, false, Tema.Gris);
                    lblTitulo.Location = new Point(14, 12);
                    var lblValor = Tema.Etiqueta(k[1], 18, true, Tema.Verde);
                    lblValor.Location = new Point(14, 36);
                    pnlTarjeta.Controls.Add(lblTitulo);
                    pnlTarjeta.Controls.Add(lblValor);
                    pnlTarjetas.Controls.Add(pnlTarjeta);
                }
                Controls.Add(pnlTarjetas);
            }
        }

        private void ColorearEstado(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (dgvTabla.Columns[e.ColumnIndex].Name != "Estado") return;
            string txt = e.Value.ToString();
            bool malo = txt.Contains("Bajo") || txt.Contains("Sin stock") || txt.Contains("Inactivo");
            e.CellStyle.ForeColor = malo ? Tema.Rojo : Tema.Verde;
        }
    }
}
