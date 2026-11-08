using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaCaminosalDeporte.Presentacion
{
    /// <summary>Pantalla "Registrar una venta" (caso de uso principal del TP). Datos de ejemplo.</summary>
    public class UcVentas : UserControl
    {
        // nombre, talles, precio, categoría
        private static readonly string[][] Catalogo =
        {
            new[] { "Remera training", "S - M - L - XL", "15990", "Remeras" },
            new[] { "Short deportivo", "S - M - L - XL", "12990", "Shorts" },
            new[] { "Pantalón deportivo", "S - M - L - XL", "12990", "Pantalones" },
            new[] { "Buzo deportivo", "S - M - L - XL", "22990", "Buzos" },
            new[] { "Zapatillas running", "39 - 40 - 41 - 42 - 43", "49990", "Calzado" },
            new[] { "Mochila deportiva", "Único", "8990", "Accesorios" },
            new[] { "Gorra deportiva", "Único", "9990", "Accesorios" },
            new[] { "Botella deportiva", "Único", "6990", "Accesorios" }
        };

        private static readonly string[] Categorias =
            { "Todos", "Remeras", "Shorts", "Pantalones", "Buzos", "Calzado", "Accesorios" };

        private FlowLayoutPanel flpProductos;
        private TextBox txtBuscar;
        private DataGridView dgvCarrito;
        private DataGridView dgvUltimas;
        private Label lblSubtotal, lblDescMonto, lblTotal;
        private ComboBox cmbDescuento;
        private readonly List<Button> chips = new List<Button>();
        private readonly List<Button> botonesPago = new List<Button>();
        private string categoria = "Todos";
        private string medioPago = "Efectivo";
        private int comprobante = 124;

        public UcVentas()
        {
            BackColor = Tema.Fondo;

            var pnlIzq = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };
            Controls.Add(pnlIzq);                 // Fill primero
            Controls.Add(ConstruirDerecha());     // Right después
            ConstruirIzquierda(pnlIzq);

            CargarProductos();
            CargarUltimas();
            Agregar("Remera training", 15990);
            Agregar("Short deportivo", 12990);
            Agregar("Zapatillas running", 49990);
            MarcarPago(0);
        }

        // ---------------------------------------------------------- lado izquierdo
        private void ConstruirIzquierda(Panel p)
        {
            flpProductos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            p.Controls.Add(flpProductos);

            var pnlUlt = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(0, 8, 0, 0) };
            dgvUltimas = Tema.NuevaGrilla();
            dgvUltimas.Dock = DockStyle.Fill;
            foreach (var c in new[] { "Fecha", "Comprobante", "Producto", "Cant.", "Total", "Medio de pago" })
                dgvUltimas.Columns.Add(c, c);
            pnlUlt.Controls.Add(dgvUltimas);
            pnlUlt.Controls.Add(new Label
            {
                Text = "Últimas ventas",
                Dock = DockStyle.Top,
                Height = 32,
                Font = Tema.Fuente(11, true),
                ForeColor = Tema.Texto,
                TextAlign = ContentAlignment.MiddleLeft
            });
            p.Controls.Add(pnlUlt);

            var pnlBus = new Panel { Dock = DockStyle.Top, Height = 96 };
            txtBuscar = Tema.Caja("Buscar producto por nombre, talle o categoría...");
            txtBuscar.SetBounds(0, 6, 520, 30);
            txtBuscar.TextChanged += (s, e) => CargarProductos();
            pnlBus.Controls.Add(txtBuscar);

            var flpChips = new FlowLayoutPanel
            {
                Location = new Point(0, 50),
                Size = new Size(780, 40),
                BackColor = Color.Transparent
            };
            foreach (var cat in Categorias)
            {
                var chip = Tema.Boton(cat, cat == "Todos");
                chip.Size = new Size(98, 32);
                chip.Margin = new Padding(0, 0, 8, 0);
                chip.Tag = cat;
                chip.Click += (s, e) =>
                {
                    categoria = (string)((Button)s).Tag;
                    foreach (var c in chips) Tema.Marcar(c, (string)c.Tag == categoria);
                    CargarProductos();
                };
                chips.Add(chip);
                flpChips.Controls.Add(chip);
            }
            pnlBus.Controls.Add(flpChips);
            p.Controls.Add(pnlBus);
        }

        private void CargarProductos()
        {
            flpProductos.SuspendLayout();
            flpProductos.Controls.Clear();
            string q = txtBuscar.Text.Trim().ToLower();
            foreach (var prod in Catalogo)
            {
                if (categoria != "Todos" && prod[3] != categoria) continue;
                if (q.Length > 0 && (prod[0] + " " + prod[1] + " " + prod[3]).ToLower().IndexOf(q) < 0) continue;
                flpProductos.Controls.Add(CrearTarjeta(prod));
            }
            flpProductos.ResumeLayout();
        }

        private Panel CrearTarjeta(string[] prod)
        {
            decimal precio = decimal.Parse(prod[2]);
            var card = Tema.NuevaTarjeta();
            card.Size = new Size(168, 200);
            card.Margin = new Padding(0, 0, 12, 12);

            // Espacio para la foto del producto (reemplazar por un PictureBox con imagen real)
            var foto = new Label
            {
                Text = prod[3].ToUpper(),
                Bounds = new Rectangle(8, 8, 152, 80),
                BackColor = Tema.Fondo,
                ForeColor = Tema.Gris,
                Font = Tema.Fuente(9, true),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var nombre = Tema.Etiqueta(prod[0], 9.5f, true);
            nombre.Location = new Point(8, 94);
            var talles = Tema.Etiqueta("Talles: " + prod[1], 8, false, Tema.Gris);
            talles.Location = new Point(8, 118);
            var lblPrecio = Tema.Etiqueta(Tema.Dinero(precio), 12, true, Tema.Verde);
            lblPrecio.Location = new Point(8, 160);

            var btn = Tema.Boton("+");
            btn.SetBounds(126, 156, 34, 30);
            btn.Click += (s, e) => Agregar(prod[0], precio);

            card.Controls.Add(foto);
            card.Controls.Add(nombre);
            card.Controls.Add(talles);
            card.Controls.Add(lblPrecio);
            card.Controls.Add(btn);
            return card;
        }

        private void CargarUltimas()
        {
            dgvUltimas.Rows.Add("27/10 - 09:45", "000124", "Remera training (L)", "1", "$ 15.990", "Tarjeta");
            dgvUltimas.Rows.Add("26/10 - 18:37", "000123", "Short deportivo (M)", "1", "$ 12.990", "Efectivo");
            dgvUltimas.Rows.Add("26/10 - 17:10", "000122", "Zapatillas running (42)", "1", "$ 49.990", "QR");
            dgvUltimas.Rows.Add("25/10 - 16:23", "000121", "Mochila deportiva", "1", "$ 8.990", "Tarjeta");
        }

        // ---------------------------------------------------------- lado derecho
        private Panel ConstruirDerecha()
        {
            var pnl = Tema.NuevaTarjeta();
            pnl.Dock = DockStyle.Right;
            pnl.Width = 400;
            pnl.Padding = new Padding(16);

            // Carrito (Fill primero)
            dgvCarrito = Tema.NuevaGrilla();
            dgvCarrito.Dock = DockStyle.Fill;
            dgvCarrito.Columns.Add("Producto", "Producto");
            dgvCarrito.Columns.Add("Cant.", "Cant.");
            dgvCarrito.Columns.Add("Subtotal", "Subtotal");
            dgvCarrito.Columns[0].FillWeight = 60;
            dgvCarrito.Columns[1].FillWeight = 20;
            dgvCarrito.Columns[2].FillWeight = 35;
            pnl.Controls.Add(dgvCarrito);

            // Pie: totales, medio de pago y botón finalizar
            var pie = new Panel { Dock = DockStyle.Bottom, Height = 290, BackColor = Color.Transparent };

            pie.Controls.Add(Izq("Subtotal", 6));
            lblSubtotal = Der(2, 11, false, Tema.Texto);
            pie.Controls.Add(lblSubtotal);

            pie.Controls.Add(Izq("Descuento", 42));
            cmbDescuento = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.Tarjeta,
                ForeColor = Tema.Texto,
                Font = Tema.Fuente(10),
                Bounds = new Rectangle(90, 38, 80, 28)
            };
            cmbDescuento.Items.AddRange(new object[] { "0%", "5%", "10%", "15%" });
            cmbDescuento.SelectedIndex = 0;
            cmbDescuento.SelectedIndexChanged += (s, e) => Recalcular();
            pie.Controls.Add(cmbDescuento);
            lblDescMonto = Der(38, 11, false, Tema.Texto);
            pie.Controls.Add(lblDescMonto);

            var lblT = Tema.Etiqueta("Total", 15, true);
            lblT.Location = new Point(0, 84);
            pie.Controls.Add(lblT);
            lblTotal = Der(80, 18, true, Tema.Verde);
            lblTotal.Height = 34;
            pie.Controls.Add(lblTotal);

            var lblMedio = Tema.Etiqueta("Medio de pago", 9.5f, false, Tema.Gris);
            lblMedio.Location = new Point(0, 130);
            pie.Controls.Add(lblMedio);

            string[] medios = { "Efectivo", "Tarjeta", "QR", "Go Cuotas" };
            for (int i = 0; i < medios.Length; i++)
            {
                var b = Tema.Boton(medios[i], false);
                b.Font = Tema.Fuente(9, true);
                b.SetBounds(i * 94, 156, 86, 52);
                b.Tag = medios[i];
                int idx = i;
                b.Click += (s, e) => MarcarPago(idx);
                botonesPago.Add(b);
                pie.Controls.Add(b);
            }

            var btnFinalizar = Tema.Boton("Finalizar venta");
            btnFinalizar.Font = Tema.Fuente(11, true);
            btnFinalizar.SetBounds(0, 224, 368, 52);
            btnFinalizar.Click += FinalizarVenta;
            pie.Controls.Add(btnFinalizar);
            pnl.Controls.Add(pie);

            // Encabezado
            var cab = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            var titulo = Tema.Etiqueta("Productos en la venta", 12, true);
            titulo.Location = new Point(0, 6);
            cab.Controls.Add(titulo);
            var btnQuitar = Tema.Boton("Quitar", false);
            btnQuitar.Dock = DockStyle.Right;
            btnQuitar.Width = 80;
            btnQuitar.Click += (s, e) =>
            {
                if (dgvCarrito.CurrentRow != null) dgvCarrito.Rows.Remove(dgvCarrito.CurrentRow);
                Recalcular();
            };
            cab.Controls.Add(btnQuitar);
            pnl.Controls.Add(cab);

            return pnl;
        }

        private Label Izq(string texto, int y)
        {
            var l = Tema.Etiqueta(texto, 10, false, Tema.Gris);
            l.Location = new Point(0, y);
            return l;
        }

        private Label Der(int y, float tam, bool negrita, Color color)
        {
            return new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(118, y, 250, 28),
                TextAlign = ContentAlignment.MiddleRight,
                Font = Tema.Fuente(tam, negrita),
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        // ---------------------------------------------------------- lógica de pantalla (demo)
        private void Agregar(string nombre, decimal precio)
        {
            foreach (DataGridViewRow r in dgvCarrito.Rows)
            {
                if ((string)r.Cells[0].Value == nombre)
                {
                    r.Cells[1].Value = (int)r.Cells[1].Value + 1;
                    ActualizarFila(r);
                    Recalcular();
                    return;
                }
            }
            int i = dgvCarrito.Rows.Add(nombre, 1, "");
            dgvCarrito.Rows[i].Tag = precio;
            ActualizarFila(dgvCarrito.Rows[i]);
            Recalcular();
        }

        private void ActualizarFila(DataGridViewRow r)
        {
            r.Cells[2].Value = Tema.Dinero((decimal)r.Tag * (int)r.Cells[1].Value);
        }

        private decimal Subtotal()
        {
            decimal sub = 0;
            foreach (DataGridViewRow r in dgvCarrito.Rows)
                sub += (decimal)r.Tag * (int)r.Cells[1].Value;
            return sub;
        }

        private void Recalcular()
        {
            decimal sub = Subtotal();
            int pct = int.Parse(((string)cmbDescuento.SelectedItem).TrimEnd('%'));
            decimal desc = sub * pct / 100;
            lblSubtotal.Text = Tema.Dinero(sub);
            lblDescMonto.Text = Tema.Dinero(desc);
            lblTotal.Text = Tema.Dinero(sub - desc);
        }

        private void MarcarPago(int indice)
        {
            for (int i = 0; i < botonesPago.Count; i++) Tema.Marcar(botonesPago[i], i == indice);
            medioPago = (string)botonesPago[indice].Tag;
        }

        private void FinalizarVenta(object sender, EventArgs e)
        {
            if (dgvCarrito.Rows.Count == 0)
            {
                MessageBox.Show("Agregá al menos un producto a la venta.", "Registrar venta",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // TODO (capa Negocio): validar stock y guardar Venta + DetalleVenta + MovimientoStock en una transacción.
            string total = lblTotal.Text;
            int unidades = 0;
            foreach (DataGridViewRow r in dgvCarrito.Rows) unidades += (int)r.Cells[1].Value;
            string primero = (string)dgvCarrito.Rows[0].Cells[0].Value;
            if (dgvCarrito.Rows.Count > 1) primero += " y otros";

            comprobante++;
            dgvUltimas.Rows.Insert(0, DateTime.Now.ToString("dd/MM - HH:mm"), comprobante.ToString("000000"),
                primero, unidades.ToString(), total, medioPago);

            MessageBox.Show("Venta registrada correctamente.\nTotal: " + total + "\nMedio de pago: " + medioPago,
                "Registrar venta", MessageBoxButtons.OK, MessageBoxIcon.Information);

            dgvCarrito.Rows.Clear();
            cmbDescuento.SelectedIndex = 0;
            Recalcular();
        }
    }
}
