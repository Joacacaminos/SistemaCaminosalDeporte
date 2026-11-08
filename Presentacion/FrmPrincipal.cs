using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Linq;
using SistemaCaminosalDeporte.Entidades;
using SistemaCaminosalDeporte.Negocio;

namespace SistemaCaminosalDeporte.Presentacion
{
    public partial class FrmPrincipal : Form  
    {
        private static readonly string[] Secciones =
            { "Inicio", "Ventas", "Productos", "Stock", "Reportes", "Usuarios", "Configuración" };

        private readonly string usuario;
        private readonly string rol;
        private readonly List<Button> botones = new List<Button>();
        private Panel pnlContenido;
        private Label lblTitulo, lblSubtitulo, lblFecha;

        public bool SolicitoCerrarSesion { get; private set; }

        public FrmPrincipal(string usuario, string rol)
        {
            this.usuario = string.IsNullOrEmpty(usuario) ? "Usuario" : usuario;
            this.rol = rol;

            Text = "Caminos Ropa Deportiva - Sistema de gestión";
            ClientSize = new Size(1280, 760);
            MinimumSize = new Size(1100, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Tema.Fondo;

            // Fill primero, luego los bordes
            pnlContenido = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            Controls.Add(pnlContenido);
            Controls.Add(ConstruirEncabezado());
            Controls.Add(ConstruirMenu());

            Navegar("Inicio");
        }
    }

internal sealed class UcProductosReales : UserControl
{
    private readonly ProductoNegocio negocio = new ProductoNegocio();
    private readonly DataGridView grilla;

    public UcProductosReales()
    {
        BackColor = Tema.Fondo;
        grilla = Tema.NuevaGrilla();
        grilla.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Id", "Nombre", "Tipo", "Talle", "Color", "Precio", "Stock" })
            grilla.Columns.Add(columna, columna);
        Controls.Add(grilla);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        AgregarBoton(barra, "Nuevo producto", true, NuevoProducto);
        AgregarBoton(barra, "Editar", false, EditarProducto);
        AgregarBoton(barra, "Eliminar", false, EliminarProducto);
        Controls.Add(barra);
        CargarProductos();
    }

    private static void AgregarBoton(FlowLayoutPanel barra, string texto, bool primario, EventHandler accion)
    {
        var boton = Tema.Boton(texto, primario);
        boton.AutoSize = true;
        boton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        boton.Padding = new Padding(10, 0, 10, 0);
        boton.Margin = new Padding(0, 0, 8, 0);
        boton.Click += accion;
        barra.Controls.Add(boton);
    }

    private void CargarProductos()
    {
        var resultado = negocio.Listar();
        if (!resultado.Exito)
        {
            MessageBox.Show(resultado.Mensaje, "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        grilla.Rows.Clear();
        foreach (var producto in resultado.Valor.OrderBy(x => x.Nombre))
        {
            var fila = grilla.Rows[grilla.Rows.Add(
                producto.idProducto,
                producto.Nombre,
                producto.Tipo,
                producto.Talle,
                producto.Color,
                Tema.Dinero(producto.Precio),
                producto.Stock)];
            fila.Tag = producto;
        }
    }

    private Producto ProductoSeleccionado()
    {
        var fila = grilla.CurrentRow;
        return fila == null ? null : fila.Tag as Producto;
    }

    private void NuevoProducto(object sender, EventArgs e)
    {
        using (var formulario = new FrmProductoModal(null))
        {
            if (formulario.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var resultado = negocio.Alta(formulario.Producto);
            MostrarResultado(resultado, "Nuevo producto");
            if (resultado.Exito)
                CargarProductos();
        }
    }

    private void EditarProducto(object sender, EventArgs e)
    {
        var seleccionado = ProductoSeleccionado();
        if (seleccionado == null)
        {
            MessageBox.Show("Seleccioná un producto para editar.", "Productos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (var formulario = new FrmProductoModal(seleccionado))
        {
            if (formulario.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var resultado = negocio.Modificar(formulario.Producto);
            MostrarResultado(resultado, "Editar producto");
            if (resultado.Exito)
                CargarProductos();
        }
    }

    private void EliminarProducto(object sender, EventArgs e)
    {
        var seleccionado = ProductoSeleccionado();
        if (seleccionado == null)
        {
            MessageBox.Show("Seleccioná un producto para eliminar.", "Productos",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirmar = MessageBox.Show(
            "¿Querés dar de baja el producto \"" + seleccionado.Nombre + "\"?",
            "Eliminar producto", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmar != DialogResult.Yes)
            return;

        var resultado = negocio.BajaLogica(seleccionado.idProducto);
        MostrarResultado(resultado, "Eliminar producto");
        if (resultado.Exito)
            CargarProductos();
    }

    private static void MostrarResultado(Resultado resultado, string titulo)
    {
        if (!resultado.Exito)
            MessageBox.Show(resultado.Mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            MessageBox.Show(resultado.Mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void MostrarResultado(Resultado<Producto> resultado, string titulo)
    {
        if (!resultado.Exito)
            MessageBox.Show(resultado.Mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            MessageBox.Show(resultado.Mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

internal sealed class UcStockReal : UserControl
{
    private readonly ProductoNegocio productos = new ProductoNegocio();
    private readonly StockNegocio stock = new StockNegocio();
    private readonly DataGridView grilla;
    private readonly Label lblPrendas;
    private readonly Label lblBajo;
    private readonly Label lblSinStock;

    public UcStockReal()
    {
        BackColor = Tema.Fondo;
        grilla = Tema.NuevaGrilla();
        grilla.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Producto", "Talle", "Color", "Stock", "Estado" })
            grilla.Columns.Add(columna, columna);
        grilla.CellFormatting += ColorearEstado;
        Controls.Add(grilla);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        AgregarBoton(barra, "Registrar ingreso de mercadería", true, IngresarMercaderia);
        AgregarBoton(barra, "Ajustar cantidad", false, AjustarCantidad);
        Controls.Add(barra);

        var resumen = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 104, BackColor = Color.Transparent };
        lblPrendas = AgregarResumen(resumen, "Prendas en stock");
        lblBajo = AgregarResumen(resumen, "Productos con stock bajo");
        lblSinStock = AgregarResumen(resumen, "Sin stock");
        Controls.Add(resumen);
        CargarStock();
    }

    private static void AgregarBoton(FlowLayoutPanel barra, string texto, bool primario, EventHandler accion)
    {
        var boton = Tema.Boton(texto, primario);
        boton.AutoSize = true;
        boton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        boton.Padding = new Padding(10, 0, 10, 0);
        boton.Margin = new Padding(0, 0, 8, 0);
        boton.Click += accion;
        barra.Controls.Add(boton);
    }

    private static Label AgregarResumen(FlowLayoutPanel fila, string titulo)
    {
        var tarjeta = Tema.NuevaTarjeta();
        tarjeta.Size = new Size(250, 84);
        tarjeta.Margin = new Padding(0, 0, 12, 12);
        tarjeta.Controls.Add(Tema.Etiqueta(titulo, 9.5f, false, Tema.Gris));
        tarjeta.Controls[0].Location = new Point(14, 12);
        var valor = Tema.Etiqueta("0", 18, true, Tema.Verde);
        valor.Location = new Point(14, 36);
        tarjeta.Controls.Add(valor);
        fila.Controls.Add(tarjeta);
        return valor;
    }

    private void CargarStock()
    {
        var resultado = productos.Listar();
        if (!resultado.Exito)
        {
            MessageBox.Show(resultado.Mensaje, "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lista = resultado.Valor.OrderBy(x => x.Nombre).ToList();
        grilla.Rows.Clear();
        foreach (var producto in lista)
        {
            var fila = grilla.Rows[grilla.Rows.Add(
                producto.Nombre,
                producto.Talle,
                producto.Color,
                producto.Stock,
                Estado(producto.Stock))];
            fila.Tag = producto;
        }

        lblPrendas.Text = lista.Sum(x => x.Stock).ToString();
        lblBajo.Text = lista.Count(x => x.Stock > 0 && x.Stock <= 5).ToString();
        lblSinStock.Text = lista.Count(x => x.Stock == 0).ToString();
    }

    private static string Estado(int cantidad)
    {
        if (cantidad <= 0) return "Sin stock";
        return cantidad <= 5 ? "Bajo" : "OK";
    }

    private void ColorearEstado(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.Value == null || grilla.Columns[e.ColumnIndex].Name != "Estado")
            return;
        var estado = e.Value.ToString();
        e.CellStyle.ForeColor = estado == "OK" ? Tema.Verde : Tema.Rojo;
    }

    private Producto ProductoSeleccionado()
    {
        var fila = grilla.CurrentRow;
        return fila == null ? null : fila.Tag as Producto;
    }

    private void IngresarMercaderia(object sender, EventArgs e)
    {
        var resultado = MostrarOperacion("Registrar ingreso de mercadería", "Cantidad a ingresar", false);
        if (resultado == null)
            return;
        MostrarResultado(resultado, "Stock");
        if (resultado.Exito)
            CargarStock();
    }

    private void AjustarCantidad(object sender, EventArgs e)
    {
        var seleccionado = ProductoSeleccionado();
        if (seleccionado == null)
        {
            MessageBox.Show("Seleccioná un producto para ajustar.", "Stock",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var resultado = MostrarOperacion("Ajustar cantidad", "Nueva cantidad", true, seleccionado);
        if (resultado == null)
            return;
        MostrarResultado(resultado, "Stock");
        if (resultado.Exito)
            CargarStock();
    }

    private Resultado<Producto> MostrarOperacion(string titulo, string etiqueta, bool ajuste, Producto seleccionado = null)
    {
        var disponibles = productos.Listar();
        if (!disponibles.Exito)
        {
            MessageBox.Show(disponibles.Mensaje, "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        using (var formulario = new FrmOperacionStock(disponibles.Valor, etiqueta, seleccionado))
        {
            if (formulario.ShowDialog(FindForm()) != DialogResult.OK)
                return null;
            if (SesionActual.Usuario == null)
                return Resultado<Producto>.Error("No hay una sesión de usuario activa.");

            return ajuste
                ? stock.AjustarCantidad(formulario.IdProducto, formulario.Cantidad, SesionActual.Usuario.idUsuario)
                : stock.IngresarMercaderia(formulario.IdProducto, formulario.Cantidad, SesionActual.Usuario.idUsuario);
        }
    }

    private static void MostrarResultado(Resultado<Producto> resultado, string titulo)
    {
        MessageBox.Show(resultado.Mensaje, titulo, MessageBoxButtons.OK,
            resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}

internal sealed class FrmProductoModal : Form
{
    private readonly TextBox txtNombre = Tema.Caja("Nombre");
    private readonly TextBox txtTipo = Tema.Caja("Tipo");
    private readonly TextBox txtTalle = Tema.Caja("Talle");
    private readonly TextBox txtColor = Tema.Caja("Color");
    private readonly TextBox txtPrecio = Tema.Caja("Precio");
    private readonly TextBox txtStock = Tema.Caja("Stock inicial");
    private readonly int idProducto;

    public Producto Producto { get; private set; }

    public FrmProductoModal(Producto existente)
    {
        idProducto = existente == null ? 0 : existente.idProducto;
        Text = existente == null ? "Nuevo producto" : "Editar producto";
        ClientSize = new Size(430, 410);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Tema.Fondo;

        AgregarCampo("Nombre", txtNombre, 20);
        AgregarCampo("Tipo", txtTipo, 68);
        AgregarCampo("Talle", txtTalle, 116);
        AgregarCampo("Color", txtColor, 164);
        AgregarCampo("Precio", txtPrecio, 212);
        AgregarCampo("Stock inicial", txtStock, 260);
        if (existente != null)
        {
            txtNombre.Text = existente.Nombre;
            txtTipo.Text = existente.Tipo;
            txtTalle.Text = existente.Talle;
            txtColor.Text = existente.Color;
            txtPrecio.Text = existente.Precio.ToString("0.##", Tema.Ar);
            txtStock.Text = existente.Stock.ToString();
        }

        var cancelar = Tema.Boton("Cancelar", false);
        cancelar.SetBounds(210, 340, 95, 40);
        cancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelar);
        var guardar = Tema.Boton("Guardar");
        guardar.SetBounds(315, 340, 95, 40);
        guardar.Click += Guardar;
        Controls.Add(guardar);
        AcceptButton = guardar;
        CancelButton = cancelar;
    }

    private void AgregarCampo(string etiqueta, TextBox caja, int y)
    {
        var label = Tema.Etiqueta(etiqueta, 9.5f, false, Tema.Gris);
        label.SetBounds(20, y, 150, 22);
        caja.SetBounds(180, y - 3, 230, 28);
        Controls.Add(label);
        Controls.Add(caja);
    }

    private void Guardar(object sender, EventArgs e)
    {
        decimal precio;
        int stock;
        if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtTipo.Text) ||
            string.IsNullOrWhiteSpace(txtTalle.Text) || string.IsNullOrWhiteSpace(txtColor.Text))
        {
            MessageBox.Show("Nombre, tipo, talle y color son obligatorios.", "Producto",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!decimal.TryParse(txtPrecio.Text.Trim(), NumberStyles.Number, Tema.Ar, out precio) || precio <= 0)
        {
            MessageBox.Show("El precio debe ser un número mayor a cero.", "Producto",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!int.TryParse(txtStock.Text.Trim(), out stock) || stock < 0)
        {
            MessageBox.Show("El stock inicial debe ser un número entero igual o mayor a cero.", "Producto",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Producto = new Producto
        {
            idProducto = idProducto,
            Nombre = txtNombre.Text.Trim(),
            Tipo = txtTipo.Text.Trim(),
            Talle = txtTalle.Text.Trim(),
            Color = txtColor.Text.Trim(),
            Precio = precio,
            Stock = stock,
            Activo = true
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class FrmOperacionStock : Form
{
    private readonly ComboBox cmbProducto = new ComboBox();
    private readonly NumericUpDown nudCantidad = new NumericUpDown();
    private readonly bool productoFijado;

    public int IdProducto { get; private set; }
    public int Cantidad { get { return (int)nudCantidad.Value; } }

    public FrmOperacionStock(List<Producto> productos, string etiqueta, Producto seleccionado)
    {
        productoFijado = seleccionado != null;
        Text = etiqueta;
        ClientSize = new Size(470, 190);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Tema.Fondo;

        var lblProducto = Tema.Etiqueta("Producto", 9.5f, false, Tema.Gris);
        lblProducto.SetBounds(20, 20, 150, 22);
        cmbProducto.SetBounds(180, 17, 270, 28);
        cmbProducto.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbProducto.BackColor = Tema.Tarjeta;
        cmbProducto.ForeColor = Tema.Texto;
        cmbProducto.Font = Tema.Fuente(10);
        cmbProducto.DisplayMember = "Nombre";
        foreach (var producto in productos)
            cmbProducto.Items.Add(producto);
        Controls.Add(lblProducto);
        Controls.Add(cmbProducto);

        var lblCantidad = Tema.Etiqueta(etiqueta, 9.5f, false, Tema.Gris);
        lblCantidad.SetBounds(20, 68, 150, 22);
        nudCantidad.SetBounds(180, 65, 120, 28);
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 1000000;
        Controls.Add(lblCantidad);
        Controls.Add(nudCantidad);

        if (seleccionado != null)
        {
            cmbProducto.SelectedItem = productos.FirstOrDefault(x => x.idProducto == seleccionado.idProducto);
            nudCantidad.Minimum = 0;
            nudCantidad.Value = seleccionado.Stock;
        }
        else if (cmbProducto.Items.Count > 0)
            cmbProducto.SelectedIndex = 0;

        cmbProducto.Enabled = !productoFijado;
        var cancelar = Tema.Boton("Cancelar", false);
        cancelar.SetBounds(250, 130, 95, 40);
        cancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelar);
        var aceptar = Tema.Boton("Aceptar");
        aceptar.SetBounds(355, 130, 95, 40);
        aceptar.Click += Aceptar;
        Controls.Add(aceptar);
        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private void Aceptar(object sender, EventArgs e)
    {
        var producto = cmbProducto.SelectedItem as Producto;
        if (producto == null)
        {
            MessageBox.Show("Seleccioná un producto.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        IdProducto = producto.idProducto;
        if (IdProducto <= 0 || (!productoFijado && Cantidad <= 0))
        {
            MessageBox.Show("Seleccioná un producto y una cantidad válida.", "Stock",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}

public partial class FrmPrincipal
{
        // ---------------------------------------------------------- menú lateral
        private Panel ConstruirMenu()
        {
            var menu = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Tema.Panel };

            var logo = Tema.Etiqueta("CAMINOS", 18, true, Tema.Verde);
            logo.Location = new Point(16, 16);
            var sub = Tema.Etiqueta("ROPA DEPORTIVA", 8, false, Tema.Gris);
            sub.Location = new Point(18, 50);
            menu.Controls.Add(logo);
            menu.Controls.Add(sub);

            int y = 90;
            foreach (var nombre in Secciones)
            {
                if (!Permitido(nombre)) continue;
                var b = new Button
                {
                    Text = "   " + nombre,
                    Tag = nombre,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = Tema.Fuente(10.5f, true),
                    Cursor = Cursors.Hand,
                    Bounds = new Rectangle(10, y, 190, 42)
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => Navegar((string)((Button)s).Tag);
                botones.Add(b);
                menu.Controls.Add(b);
                y += 48;
            }

            var lema = Tema.Etiqueta("Más deporte, mejor vida", 8.5f, false, Tema.Verde);
            lema.Dock = DockStyle.Bottom;
            lema.AutoSize = false;
            lema.Height = 36;
            lema.TextAlign = ContentAlignment.MiddleCenter;

            var salir = new Button
            {
                Text = "   Cerrar sesión",
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Tema.Fuente(10),
                ForeColor = Tema.Gris,
                BackColor = Tema.Panel,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Bottom,
                Height = 46
            };
            salir.FlatAppearance.BorderSize = 0;
            salir.Click += (s, e) => CerrarSesion();

            menu.Controls.Add(lema);
            menu.Controls.Add(salir);
            return menu;
        }

        private void CerrarSesion()
        {
            SolicitoCerrarSesion = true;
            SesionActual.Cerrar();
            Close();
        }

        /// <summary>Qué ve cada rol (ajustable según lo que defina el equipo).</summary>
        private bool Permitido(string seccion)
        {
            if (rol == "Administrador") return true;
            if (rol == "Dueño") return seccion != "Usuarios" && seccion != "Configuración";
            return seccion == "Inicio" || seccion == "Ventas" || seccion == "Productos" || seccion == "Stock";
        }

        // ---------------------------------------------------------- encabezado
        private Panel ConstruirEncabezado()
        {
            var cab = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Panel };

            lblTitulo = Tema.Etiqueta("", 16, true);
            lblTitulo.Location = new Point(20, 8);
            lblSubtitulo = Tema.Etiqueta("", 9, false, Tema.Gris);
            lblSubtitulo.Location = new Point(22, 38);

            var lblUsuario = new Label
            {
                Text = usuario + "  (" + rol + ")",
                Dock = DockStyle.Right,
                Width = 260,
                TextAlign = ContentAlignment.MiddleRight,
                Font = Tema.Fuente(10, true),
                ForeColor = Tema.Texto,
                Padding = new Padding(0, 0, 16, 0)
            };
            lblFecha = new Label
            {
                Dock = DockStyle.Right,
                Width = 300,
                TextAlign = ContentAlignment.MiddleRight,
                Font = Tema.Fuente(9.5f),
                ForeColor = Tema.Gris
            };
            ActualizarFecha();
            var timer = new Timer { Interval = 30000 };
            timer.Tick += (s, e) => ActualizarFecha();
            timer.Start();

            cab.Controls.Add(lblTitulo);
            cab.Controls.Add(lblSubtitulo);
            cab.Controls.Add(lblUsuario);
            cab.Controls.Add(lblFecha);
            return cab;
        }

        private void ActualizarFecha()
        {
            lblFecha.Text = DateTime.Now.ToString("dddd, d 'de' MMMM yyyy  HH:mm", Tema.Ar);
        }

        // ---------------------------------------------------------- navegación
        private void Navegar(string seccion)
        {
            Control vista;
            string subtitulo;
            switch (seccion)
            {
                case "Ventas":
                    vista = new UcVentas();
                    subtitulo = "Registrá una nueva venta";
                    break;
                case "Productos":
                    vista = VistaProductos();
                    subtitulo = "Catálogo de prendas por tipo, talle y color";
                    break;
                case "Stock":
                    vista = VistaStock();
                    subtitulo = "Cantidades disponibles e ingreso de mercadería";
                    break;
                case "Reportes":
                    vista = VistaReportes();
                    subtitulo = "Ingresos diarios, semanales y mensuales";
                    break;
                case "Usuarios":
                    vista = VistaUsuarios();
                    subtitulo = "Accesos y roles del sistema";
                    break;
                case "Configuración":
                    vista = VistaConfiguracion();
                    subtitulo = "Copias de seguridad y ajustes";
                    break;
                default:
                    seccion = "Inicio";
                    vista = VistaInicio();
                    subtitulo = "Resumen del negocio";
                    break;
            }

            foreach (Control c in pnlContenido.Controls.Cast<Control>().ToList()) c.Dispose();
            pnlContenido.Controls.Clear();
            vista.Dock = DockStyle.Fill;
            pnlContenido.Controls.Add(vista);

            lblTitulo.Text = seccion;
            lblSubtitulo.Text = subtitulo;
            foreach (var b in botones)
            {
                bool activo = (string)b.Tag == seccion;
                b.BackColor = activo ? Tema.Verde : Tema.Panel;
                b.ForeColor = activo ? Color.Black : Tema.Gris;
            }
        }

        // ---------------------------------------------------------- vistas
        private Control VistaInicio()
        {
            return new UcTabla(
                new[]
                {
                    new[] { "Ventas de hoy", "$ 78.970" },
                    new[] { "Ventas de la semana", "$ 412.300" },
                    new[] { "Ventas del mes", "$ 1.640.500" },
                    new[] { "Productos con stock bajo", "4" }
                },
                null,
                new[] { "Fecha", "Comprobante", "Producto", "Cant.", "Total", "Medio de pago" },
                new[]
                {
                    new[] { "27/10 - 09:45", "000124", "Remera training (L)", "1", "$ 15.990", "Tarjeta" },
                    new[] { "26/10 - 18:37", "000123", "Short deportivo (M)", "1", "$ 12.990", "Efectivo" },
                    new[] { "26/10 - 17:10", "000122", "Zapatillas running (42)", "1", "$ 49.990", "QR" },
                    new[] { "25/10 - 16:23", "000121", "Mochila deportiva", "1", "$ 8.990", "Tarjeta" }
                });
        }

        private Control VistaProductos()
        {
            return new UcProductosReales();
        }

        private Control VistaStock()
        {
            return new UcStockReal();
        }

        private Control VistaReportes()
        {
            return new UcTabla(
                new[]
                {
                    new[] { "Ingresos de hoy", "$ 78.970" },
                    new[] { "Ingresos de la semana", "$ 412.300" },
                    new[] { "Ingresos del mes", "$ 1.640.500" }
                },
                new[] { "Diario", "Semanal", "Mensual", "Exportar" },
                new[] { "Fecha", "Ventas", "Efectivo", "Tarjeta", "QR", "Go Cuotas", "Total" },
                new[]
                {
                    new[] { "27/10/2025", "6", "$ 25.980", "$ 31.980", "$ 12.990", "$ 8.020", "$ 78.970" },
                    new[] { "26/10/2025", "9", "$ 40.500", "$ 55.200", "$ 49.990", "$ 0", "$ 145.690" },
                    new[] { "25/10/2025", "4", "$ 15.000", "$ 28.480", "$ 0", "$ 22.990", "$ 66.470" }
                });
        }

        private Control VistaUsuarios()
        {
            return new UcTabla(
                null,
                new[] { "Nuevo usuario", "Editar", "Desactivar" },
                new[] { "Id", "Nombre", "Usuario", "Rol", "Estado" },
                new[]
                {
                    new[] { "1", "Gustavo Caminos", "gcaminos", "Dueño", "Activo" },
                    new[] { "2", "Empleado", "empleado1", "Empleado", "Activo" },
                    new[] { "3", "Administrador", "admin", "Administrador", "Activo" }
                });
        }

        private Control VistaConfiguracion()
        {
            var p = new Panel { BackColor = Tema.Fondo };
            var tarjeta = Tema.NuevaTarjeta();
            tarjeta.SetBounds(0, 0, 460, 190);

            var t = Tema.Etiqueta("Copia de seguridad", 12, true);
            t.Location = new Point(20, 16);
            var d = Tema.Etiqueta("Guarda una copia de la base de datos para evitar pérdida de información.", 9, false, Tema.Gris);
            d.Location = new Point(20, 46);
            var btnCopia = Tema.Boton("Crear copia ahora");
            btnCopia.SetBounds(20, 84, 200, 42);
            btnCopia.Click += (s, e) => MessageBox.Show("Copia de seguridad creada (demo).", "Configuración",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            var btnRest = Tema.Boton("Restaurar copia", false);
            btnRest.SetBounds(230, 84, 200, 42);

            tarjeta.Controls.Add(t);
            tarjeta.Controls.Add(d);
            tarjeta.Controls.Add(btnCopia);
            tarjeta.Controls.Add(btnRest);
            p.Controls.Add(tarjeta);
            return p;
        }
    }
}
