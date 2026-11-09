using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using SistemaCaminosalDeporte.Entidades;
using SistemaCaminosalDeporte.Negocio;

namespace SistemaCaminosalDeporte.Presentacion
{
    /// <summary>Pantalla de registro de ventas usando productos, stock y sesión reales.</summary>
    public class UcVentas : UserControl
    {
        private static readonly string[] Categorias =
            { "Todos", "Remera", "Short", "Calza", "Buzo", "Campera", "Conjunto", "Top", "Accesorio" };

        private FlowLayoutPanel flpProductos;
        private TextBox txtBuscar;
        private DataGridView dgvCarrito;
        private DataGridView dgvUltimas;
        private Label lblSubtotal, lblDescMonto, lblRecargo, lblTotal;
        private ComboBox cmbDescuento;
        private ComboBox cmbCuotaPlan;
        private readonly List<Button> btnCategorias = new List<Button>();
        private readonly List<Button> btnMediosPago = new List<Button>();
        private readonly ProductoNegocio productoNegocio = new ProductoNegocio();
        private readonly VentaNegocio ventaNegocio = new VentaNegocio();
        private string categoria = "Todos";
        private string medioPago = "Efectivo";

        private sealed class CarritoItem
        {
            public int idProducto { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
        }

        public UcVentas()
        {
            BackColor = Tema.Fondo;

            var pnlIzq = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };
            Controls.Add(pnlIzq);                 // Fill primero
            Controls.Add(ConstruirDerecha());     // Right después
            ConstruirIzquierda(pnlIzq);

            CargarProductos();
            CargarUltimas();
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
                var btnChip = Tema.Boton(cat, cat == "Todos");
                btnChip.Size = new Size(98, 32);
                btnChip.Margin = new Padding(0, 0, 8, 0);
                btnChip.Tag = cat;
                btnChip.Click += (s, e) =>
                {
                    categoria = (string)((Button)s).Tag;
                    foreach (var btnCategoria in btnCategorias) Tema.Marcar(btnCategoria, (string)btnCategoria.Tag == categoria);
                    CargarProductos();
                };
                btnCategorias.Add(btnChip);
                flpChips.Controls.Add(btnChip);
            }
            pnlBus.Controls.Add(flpChips);
            p.Controls.Add(pnlBus);
        }

        private void CargarProductos()
        {
            flpProductos.SuspendLayout();
            flpProductos.Controls.Clear();
            var resultado = productoNegocio.Buscar(txtBuscar.Text);
            if (!resultado.Exito)
            {
                flpProductos.ResumeLayout();
                return;
            }

            foreach (var prod in resultado.Valor.Where(x => x.Activo && x.Stock > 0 &&
                (categoria == "Todos" || string.Equals(x.Tipo, categoria, StringComparison.OrdinalIgnoreCase))))
            {
                flpProductos.Controls.Add(CrearTarjeta(prod));
            }
            flpProductos.ResumeLayout();
        }

        private Panel CrearTarjeta(Producto prod)
        {
            var card = Tema.NuevaTarjeta();
            card.Size = new Size(168, 200);
            card.Margin = new Padding(0, 0, 12, 12);
            card.Tag = prod.idProducto;

            // Espacio para la foto del producto (reemplazar por un PictureBox con imagen real)
            var lblFoto = new Label
            {
                Text = prod.Tipo.ToUpper(),
                Bounds = new Rectangle(8, 8, 152, 80),
                BackColor = Tema.Fondo,
                ForeColor = Tema.Gris,
                Font = Tema.Fuente(9, true),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblNombre = Tema.Etiqueta(prod.Nombre, 9.5f, true);
            lblNombre.Location = new Point(8, 94);
            var lblTalles = Tema.Etiqueta("Talle: " + prod.Talle + "  Stock: " + prod.Stock, 8, false, Tema.Gris);
            lblTalles.Location = new Point(8, 118);
            var lblPrecio = Tema.Etiqueta(Tema.Dinero(prod.Precio), 12, true, Tema.Verde);
            lblPrecio.Location = new Point(8, 160);

            var btnAgregar = Tema.Boton("+");
            btnAgregar.SetBounds(126, 156, 34, 30);
            btnAgregar.Enabled = prod.Stock > 0;
            btnAgregar.Click += (s, e) => Agregar(prod);
            var btnRestar = Tema.Boton("-");
            btnRestar.SetBounds(88, 156, 34, 30);
            btnRestar.Click += (s, e) => Restar(prod.idProducto);

            card.Controls.Add(lblFoto);
            card.Controls.Add(lblNombre);
            card.Controls.Add(lblTalles);
            card.Controls.Add(lblPrecio);
            card.Controls.Add(btnRestar);
            card.Controls.Add(btnAgregar);
            return card;
        }

        private void CargarUltimas()
        {
            dgvUltimas.Rows.Clear();
            var resultado = ventaNegocio.UltimasVentas();
            if (!resultado.Exito)
                return;

            foreach (var venta in resultado.Valor)
            {
                dgvUltimas.Rows.Add(
                    venta.Fecha.ToString("dd/MM - HH:mm"),
                    venta.idVenta.ToString("000000"),
                    venta.Producto,
                    venta.Cantidad.ToString(),
                    Tema.Dinero(venta.Total),
                    venta.MedioPago);
            }
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
            var colQuitar = new DataGridViewButtonColumn
            {
                Name = "Quitar",
                HeaderText = "",
                Text = "Quitar",
                UseColumnTextForButtonValue = true
            };
            dgvCarrito.Columns.Add(colQuitar);
            dgvCarrito.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvCarrito.Columns["Quitar"].Index)
                {
                    dgvCarrito.Rows.RemoveAt(e.RowIndex);
                    Recalcular();
                }
            };
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

            pie.Controls.Add(Izq("Recargo", 76));
            lblRecargo = Der(72, 11, false, Tema.Texto);
            pie.Controls.Add(lblRecargo);

            var lblT = Tema.Etiqueta("Total", 15, true);
            lblT.Location = new Point(0, 108);
            pie.Controls.Add(lblT);
            lblTotal = Der(104, 18, true, Tema.Verde);
            lblTotal.Height = 34;
            pie.Controls.Add(lblTotal);

            var lblMedio = Tema.Etiqueta("Medio de pago", 9.5f, false, Tema.Gris);
            lblMedio.Location = new Point(0, 144);
            pie.Controls.Add(lblMedio);

            cmbCuotaPlan = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.Tarjeta,
                ForeColor = Tema.Texto,
                Font = Tema.Fuente(9),
                Bounds = new Rectangle(190, 140, 178, 28),
                Visible = false
            };
            cmbCuotaPlan.SelectedIndexChanged += (s, e) =>
            {
                cuotaPlan = cmbCuotaPlan.SelectedItem as string;
                Recalcular();
            };
            pie.Controls.Add(cmbCuotaPlan);

            string[] medios = { "Efectivo", "Tarjeta", "QR", "Go Cuotas" };
            for (int i = 0; i < medios.Length; i++)
            {
                var btnMedioPago = Tema.Boton(medios[i], false);
                btnMedioPago.Font = Tema.Fuente(9, true);
                btnMedioPago.SetBounds(i * 94, 170, 86, 52);
                btnMedioPago.Tag = medios[i];
                int idx = i;
                btnMedioPago.Click += (s, e) => MarcarPago(idx);
                btnMediosPago.Add(btnMedioPago);
                pie.Controls.Add(btnMedioPago);
            }

            var btnFinalizar = Tema.Boton("Finalizar venta");
            btnFinalizar.Font = Tema.Fuente(11, true);
            btnFinalizar.SetBounds(0, 238, 368, 52);
            btnFinalizar.Click += FinalizarVenta;
            pie.Controls.Add(btnFinalizar);
            pnl.Controls.Add(pie);

            // Encabezado
            var pnlCabecera = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            var lblTitulo = Tema.Etiqueta("Productos en la venta", 12, true);
            lblTitulo.Location = new Point(0, 6);
            pnlCabecera.Controls.Add(lblTitulo);
            pnl.Controls.Add(pnlCabecera);

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

        // ---------------------------------------------------------- lógica de pantalla
        private void Agregar(Producto producto)
        {
            foreach (DataGridViewRow r in dgvCarrito.Rows)
            {
                var item = (CarritoItem)r.Tag;
                if (item.idProducto == producto.idProducto)
                {
                    var nuevaCantidad = item.Cantidad + 1;
                    if (nuevaCantidad > producto.Stock)
                    {
                        MessageBox.Show("No hay stock suficiente para agregar otra unidad.", "Stock",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    item.Cantidad = nuevaCantidad;
                    r.Cells[1].Value = nuevaCantidad;
                    ActualizarFila(r);
                    Recalcular();
                    return;
                }
            }
            if (producto.Stock <= 0)
            {
                MessageBox.Show("El producto no tiene stock disponible.", "Stock",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int i = dgvCarrito.Rows.Add(producto.Nombre, 1, "");
            dgvCarrito.Rows[i].Tag = new CarritoItem
            {
                idProducto = producto.idProducto,
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Cantidad = 1
            };
            ActualizarFila(dgvCarrito.Rows[i]);
            Recalcular();
        }

        private void ActualizarFila(DataGridViewRow r)
        {
            var item = (CarritoItem)r.Tag;
            r.Cells[1].Value = item.Cantidad;
            r.Cells[2].Value = Tema.Dinero(item.Precio * item.Cantidad);
        }

        private decimal Subtotal()
        {
            decimal sub = 0;
            foreach (DataGridViewRow r in dgvCarrito.Rows)
            {
                var item = (CarritoItem)r.Tag;
                sub += item.Precio * item.Cantidad;
            }
            return sub;
        }

        private void Recalcular()
        {
            decimal sub = Subtotal();
            int pct = int.Parse(((string)cmbDescuento.SelectedItem).TrimEnd('%'));
            var pago = ventaNegocio.CalcularPago(sub, medioPago, cuotaPlan, pct);
            lblSubtotal.Text = Tema.Dinero(pago.Subtotal);
            lblDescMonto.Text = Tema.Dinero(pago.Descuento);
            lblRecargo.Text = Tema.Dinero(pago.Recargo);
            lblTotal.Text = Tema.Dinero(pago.Total);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // UcVentas
            // 
            this.Name = "UcVentas";
            this.Load += new System.EventHandler(this.UcVentas_Load);
            this.ResumeLayout(false);

        }

        private void UcVentas_Load(object sender, EventArgs e)
        {

        }

        private void MarcarPago(int indice)
        {
            for (int i = 0; i < btnMediosPago.Count; i++) Tema.Marcar(btnMediosPago[i], i == indice);
            medioPago = (string)btnMediosPago[indice].Tag;
            if (cmbCuotaPlan != null)
                ActualizarCuotas();
        }

        private string cuotaPlan = null;

        private void ActualizarCuotas()
        {
            cmbCuotaPlan.Items.Clear();
            if (medioPago == "Tarjeta")
                cmbCuotaPlan.Items.AddRange(new object[] { "1 cuota", "3 cuotas", "Plan Z (Tarjeta Naranja)" });
            else if (medioPago == "Go Cuotas")
                cmbCuotaPlan.Items.AddRange(new object[] { "1 cuota", "3 cuotas" });
            cmbCuotaPlan.Visible = cmbCuotaPlan.Items.Count > 0;
            if (cmbCuotaPlan.Visible)
                cmbCuotaPlan.SelectedIndex = 0;
            cuotaPlan = cmbCuotaPlan.Visible ? (string)cmbCuotaPlan.SelectedItem : null;
            Recalcular();
        }

        private void Restar(int idProducto)
        {
            foreach (DataGridViewRow fila in dgvCarrito.Rows)
            {
                var item = (CarritoItem)fila.Tag;
                if (item.idProducto != idProducto)
                    continue;
                item.Cantidad--;
                if (item.Cantidad <= 0)
                    dgvCarrito.Rows.Remove(fila);
                else
                    ActualizarFila(fila);
                Recalcular();
                return;
            }
        }

        private void FinalizarVenta(object sender, EventArgs e)
        {
            if (dgvCarrito.Rows.Count == 0)
            {
                MessageBox.Show("Agregá al menos un producto a la venta.", "Registrar venta",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var lineas = dgvCarrito.Rows.Cast<DataGridViewRow>()
                .Select(fila => new LineaVenta
                {
                    idProducto = ((CarritoItem)fila.Tag).idProducto,
                    Cantidad = ((CarritoItem)fila.Tag).Cantidad
                })
                .ToList();

            int pct = int.Parse(((string)cmbDescuento.SelectedItem).TrimEnd('%'));
            var resultado = ventaNegocio.Registrar(lineas, medioPago, cuotaPlan, pct, SesionActual.Usuario);
            if (!resultado.Exito)
            {
                MessageBox.Show(resultado.Mensaje, "Registrar venta",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CargarProductos();
                return;
            }

            string total = Tema.Dinero(resultado.Valor.MontoTotal);
            MessageBox.Show(resultado.Mensaje + "\nSubtotal: " + Tema.Dinero(resultado.Valor.MontoSubtotal)
                + "\nRecargo: " + Tema.Dinero(resultado.Valor.MontoRecargo)
                + "\nTotal: " + total + "\nMedio de pago: " + VentaNegocio.ObtenerDescripcionMedioPago(resultado.Valor),
                "Registrar venta", MessageBoxButtons.OK, MessageBoxIcon.Information);

            dgvCarrito.Rows.Clear();
            cmbDescuento.SelectedIndex = 0;
            ActualizarCuotas();
            Recalcular();
            CargarProductos();
            CargarUltimas();
        }
    }
}
