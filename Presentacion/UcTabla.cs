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
        public DataGridView Grilla { get; private set; }

        public UcTabla(string[][] kpis, string[] botones, string[] columnas, string[][] filas)
        {
            BackColor = Tema.Fondo;

            // El control "Fill" se agrega primero y los "Top" después (así WinForms acomoda bien el docking).
            Grilla = Tema.NuevaGrilla();
            Grilla.Dock = DockStyle.Fill;
            foreach (var c in columnas) Grilla.Columns.Add(c, c);
            foreach (var f in filas) Grilla.Rows.Add(f);
            Grilla.CellFormatting += ColorearEstado;
            Controls.Add(Grilla);

            if (botones != null && botones.Length > 0)
            {
                var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
                for (int i = 0; i < botones.Length; i++)
                {
                    var b = Tema.Boton(botones[i], i == 0);
                    b.AutoSize = true;
                    b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    b.Padding = new Padding(10, 0, 10, 0);
                    b.Margin = new Padding(0, 0, 8, 0);
                    barra.Controls.Add(b);
                }
                Controls.Add(barra);
            }

            if (kpis != null && kpis.Length > 0)
            {
                var fila = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 104,
                    BackColor = Color.Transparent
                };
                foreach (var k in kpis)
                {
                    var tarjeta = Tema.NuevaTarjeta();
                    tarjeta.Size = new Size(250, 84);
                    tarjeta.Margin = new Padding(0, 0, 12, 12);
                    var titulo = Tema.Etiqueta(k[0], 9.5f, false, Tema.Gris);
                    titulo.Location = new Point(14, 12);
                    var valor = Tema.Etiqueta(k[1], 18, true, Tema.Verde);
                    valor.Location = new Point(14, 36);
                    tarjeta.Controls.Add(titulo);
                    tarjeta.Controls.Add(valor);
                    fila.Controls.Add(tarjeta);
                }
                Controls.Add(fila);
            }
        }

        private void ColorearEstado(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (Grilla.Columns[e.ColumnIndex].Name != "Estado") return;
            string txt = e.Value.ToString();
            bool malo = txt.Contains("Bajo") || txt.Contains("Sin stock") || txt.Contains("Inactivo");
            e.CellStyle.ForeColor = malo ? Tema.Rojo : Tema.Verde;
        }
    }
}
