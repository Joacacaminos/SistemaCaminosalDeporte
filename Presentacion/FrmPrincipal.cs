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
    /// <summary>Ventana principal y navegación por las funcionalidades del sistema.</summary>
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

/// <summary>Vista de productos activos conectada a ProductoNegocio.</summary>
internal sealed class UcProductosReales : UserControl
{
    private readonly ProductoNegocio negocio = new ProductoNegocio();
    private readonly DataGridView dgvProductos;

    public UcProductosReales()
    {
        BackColor = Tema.Fondo;
        dgvProductos = Tema.NuevaGrilla();
        dgvProductos.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Id", "Nombre", "Tipo", "Talle", "Color", "Precio", "Stock" })
            dgvProductos.Columns.Add(columna, columna);
        Controls.Add(dgvProductos);

        var pnlAcciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        AgregarBoton(pnlAcciones, "Nuevo producto", true, NuevoProducto);
        AgregarBoton(pnlAcciones, "Editar", false, EditarProducto);
        AgregarBoton(pnlAcciones, "Eliminar", false, EliminarProducto);
        Controls.Add(pnlAcciones);
        CargarProductos();
    }

    private static void AgregarBoton(FlowLayoutPanel pnlAcciones, string texto, bool primario, EventHandler accion)
    {
        var btnAccion = Tema.Boton(texto, primario);
        btnAccion.AutoSize = true;
        btnAccion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnAccion.Padding = new Padding(10, 0, 10, 0);
        btnAccion.Margin = new Padding(0, 0, 8, 0);
        btnAccion.Click += accion;
        pnlAcciones.Controls.Add(btnAccion);
    }

    private void CargarProductos()
    {
        var resultado = negocio.Listar();
        if (!resultado.Exito)
        {
            MessageBox.Show(resultado.Mensaje, "Productos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        dgvProductos.Rows.Clear();
        foreach (var producto in resultado.Valor.OrderBy(x => x.Nombre))
        {
            var fila = dgvProductos.Rows[dgvProductos.Rows.Add(
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
        var fila = dgvProductos.CurrentRow;
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

/// <summary>Vista de existencias y movimientos conectada a StockNegocio.</summary>
internal sealed class UcStockReal : UserControl
{
    private readonly ProductoNegocio productos = new ProductoNegocio();
    private readonly StockNegocio stock = new StockNegocio();
    private readonly DataGridView dgvStock;
    private readonly Label lblPrendas;
    private readonly Label lblBajo;
    private readonly Label lblSinStock;

    public UcStockReal()
    {
        BackColor = Tema.Fondo;
        dgvStock = Tema.NuevaGrilla();
        dgvStock.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Producto", "Talle", "Color", "Stock", "Estado" })
            dgvStock.Columns.Add(columna, columna);
        dgvStock.CellFormatting += ColorearEstado;
        Controls.Add(dgvStock);

        var pnlAcciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        AgregarBoton(pnlAcciones, "Registrar ingreso de mercadería", true, IngresarMercaderia);
        AgregarBoton(pnlAcciones, "Ajustar cantidad", false, AjustarCantidad);
        Controls.Add(pnlAcciones);

        var resumen = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 104, BackColor = Color.Transparent };
        lblPrendas = AgregarResumen(resumen, "Prendas en stock");
        lblBajo = AgregarResumen(resumen, "Productos con stock bajo");
        lblSinStock = AgregarResumen(resumen, "Sin stock");
        Controls.Add(resumen);
        CargarStock();
    }

    private static void AgregarBoton(FlowLayoutPanel pnlAcciones, string texto, bool primario, EventHandler accion)
    {
        var btnAccion = Tema.Boton(texto, primario);
        btnAccion.AutoSize = true;
        btnAccion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnAccion.Padding = new Padding(10, 0, 10, 0);
        btnAccion.Margin = new Padding(0, 0, 8, 0);
        btnAccion.Click += accion;
        pnlAcciones.Controls.Add(btnAccion);
    }

    private static Label AgregarResumen(FlowLayoutPanel fila, string titulo)
    {
        var pnlTarjeta = Tema.NuevaTarjeta();
        pnlTarjeta.Size = new Size(250, 84);
        pnlTarjeta.Margin = new Padding(0, 0, 12, 12);
        pnlTarjeta.Controls.Add(Tema.Etiqueta(titulo, 9.5f, false, Tema.Gris));
        pnlTarjeta.Controls[0].Location = new Point(14, 12);
        var valor = Tema.Etiqueta("0", 18, true, Tema.Verde);
        valor.Location = new Point(14, 36);
        pnlTarjeta.Controls.Add(valor);
        fila.Controls.Add(pnlTarjeta);
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
        dgvStock.Rows.Clear();
        foreach (var producto in lista)
        {
            var fila = dgvStock.Rows[dgvStock.Rows.Add(
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
        if (e.RowIndex < 0 || e.Value == null || dgvStock.Columns[e.ColumnIndex].Name != "Estado")
            return;
        var estado = e.Value.ToString();
        e.CellStyle.ForeColor = estado == "OK" ? Tema.Verde : Tema.Rojo;
    }

    private Producto ProductoSeleccionado()
    {
        var fila = dgvStock.CurrentRow;
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

/// <summary>Formulario modal para alta y edición de productos.</summary>
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

        var btnCancelar = Tema.Boton("Cancelar", false);
        btnCancelar.SetBounds(210, 340, 95, 40);
        btnCancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(btnCancelar);
        var btnGuardar = Tema.Boton("Guardar");
        btnGuardar.SetBounds(315, 340, 95, 40);
        btnGuardar.Click += Guardar;
        Controls.Add(btnGuardar);
        AcceptButton = btnGuardar;
        CancelButton = btnCancelar;
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

/// <summary>Formulario modal para seleccionar un producto y una cantidad de stock.</summary>
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
        var btnCancelar = Tema.Boton("Cancelar", false);
        btnCancelar.SetBounds(250, 130, 95, 40);
        btnCancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(btnCancelar);
        var btnAceptar = Tema.Boton("Aceptar");
        btnAceptar.SetBounds(355, 130, 95, 40);
        btnAceptar.Click += Aceptar;
        Controls.Add(btnAceptar);
        AcceptButton = btnAceptar;
        CancelButton = btnCancelar;
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

/// <summary>Constructores visuales compartidos por las vistas reales.</summary>
internal static class VistaRealUtil
{
    public static Button Boton(string texto, bool primario, EventHandler accion)
    {
        var btnAccion = Tema.Boton(texto, primario);
        btnAccion.AutoSize = true;
        btnAccion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnAccion.Padding = new Padding(10, 0, 10, 0);
        btnAccion.Margin = new Padding(0, 0, 8, 0);
        btnAccion.Click += accion;
        return btnAccion;
    }

    public static Label Tarjeta(FlowLayoutPanel fila, string titulo)
    {
        var pnlTarjeta = Tema.NuevaTarjeta();
        pnlTarjeta.Size = new Size(250, 84);
        pnlTarjeta.Margin = new Padding(0, 0, 12, 12);
        var lblEtiqueta = Tema.Etiqueta(titulo, 9.5f, false, Tema.Gris);
        lblEtiqueta.Location = new Point(14, 12);
        var lblValor = Tema.Etiqueta("0", 18, true, Tema.Verde);
        lblValor.Location = new Point(14, 36);
        pnlTarjeta.Controls.Add(lblEtiqueta);
        pnlTarjeta.Controls.Add(lblValor);
        fila.Controls.Add(pnlTarjeta);
        return lblValor;
    }

    public static decimal Total(Resultado<List<IngresoReporte>> resultado)
    {
        return resultado != null && resultado.Exito && resultado.Valor != null
            ? resultado.Valor.Sum(x => x.Total)
            : 0m;
    }
}

/// <summary>Resumen real de ventas recientes y stock bajo.</summary>
internal sealed class UcInicioReal : UserControl
{
    private readonly ReporteNegocio reportes = new ReporteNegocio();
    private readonly StockNegocio stock = new StockNegocio();
    private readonly VentaNegocio ventas = new VentaNegocio();

    public UcInicioReal()
    {
        BackColor = Tema.Fondo;
        var dgvUltimas = Tema.NuevaGrilla();
        dgvUltimas.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Fecha", "Comprobante", "Producto", "Cant.", "Total", "Medio de pago" })
            dgvUltimas.Columns.Add(columna, columna);
        Controls.Add(dgvUltimas);

        var pnlTarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 104, BackColor = Color.Transparent };
        var hoy = DateTime.Today;
        var semana = hoy.AddDays(-((int)hoy.DayOfWeek + 6) % 7);
        var mes = new DateTime(hoy.Year, hoy.Month, 1);
        VistaRealUtil.Tarjeta(pnlTarjetas, "Ventas de hoy").Text = Tema.Dinero(VistaRealUtil.Total(reportes.IngresosPorDia(hoy, hoy)));
        VistaRealUtil.Tarjeta(pnlTarjetas, "Ventas de la semana").Text = Tema.Dinero(VistaRealUtil.Total(reportes.IngresosPorSemana(semana, hoy)));
        VistaRealUtil.Tarjeta(pnlTarjetas, "Ventas del mes").Text = Tema.Dinero(VistaRealUtil.Total(reportes.IngresosPorMes(mes, hoy)));
        var bajo = stock.ListarStockBajo(5);
        VistaRealUtil.Tarjeta(pnlTarjetas, "Productos con stock bajo").Text =
            bajo.Exito ? bajo.Valor.Count.ToString() : "0";
        Controls.Add(pnlTarjetas);

        var ultimas = ventas.UltimasVentas();
        if (!ultimas.Exito)
        {
            MessageBox.Show(ultimas.Mensaje, "Inicio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        foreach (var venta in ultimas.Valor)
            dgvUltimas.Rows.Add(venta.Fecha.ToString("dd/MM - HH:mm"), venta.idVenta.ToString("000000"),
                venta.Producto, venta.Cantidad, Tema.Dinero(venta.Total), venta.MedioPago);
    }
}

/// <summary>Reportes de ingresos y productos más vendidos.</summary>
internal sealed class UcReportesReal : UserControl
{
    private readonly ReporteNegocio negocio = new ReporteNegocio();
    private readonly DataGridView dgvIngresos = Tema.NuevaGrilla();
    private readonly DataGridView dgvProductosVendidos = Tema.NuevaGrilla();
    private readonly Label lblTotal;
    private readonly Label lblRegistros;
    private readonly Label lblProductos;
    private List<IngresoReporte> ingresosActuales = new List<IngresoReporte>();
    private string periodoActual = "Diario";

    public UcReportesReal()
    {
        BackColor = Tema.Fondo;
        dgvIngresos.Dock = DockStyle.Fill;
        dgvIngresos.Columns.Add("Periodo", "Periodo");
        dgvIngresos.Columns.Add("Total", "Total");
        Controls.Add(dgvIngresos);

        dgvProductosVendidos.Dock = DockStyle.Bottom;
        dgvProductosVendidos.Height = 180;
        dgvProductosVendidos.Columns.Add("Producto", "Producto");
        dgvProductosVendidos.Columns.Add("Cantidad", "Cantidad vendida");
        dgvProductosVendidos.Columns.Add("Total", "Ingresos");
        Controls.Add(dgvProductosVendidos);

        var pnlTarjetas = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 104, BackColor = Color.Transparent };
        lblTotal = VistaRealUtil.Tarjeta(pnlTarjetas, "Total del período");
        lblRegistros = VistaRealUtil.Tarjeta(pnlTarjetas, "Períodos");
        lblProductos = VistaRealUtil.Tarjeta(pnlTarjetas, "Productos vendidos");
        Controls.Add(pnlTarjetas);

        var pnlBarra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        pnlBarra.Controls.Add(VistaRealUtil.Boton("Diario", true, (s, e) => Cargar("Diario")));
        pnlBarra.Controls.Add(VistaRealUtil.Boton("Semanal", false, (s, e) => Cargar("Semanal")));
        pnlBarra.Controls.Add(VistaRealUtil.Boton("Mensual", false, (s, e) => Cargar("Mensual")));
        pnlBarra.Controls.Add(VistaRealUtil.Boton("Exportar", false, Exportar));
        Controls.Add(pnlBarra);
        Cargar(periodoActual);
    }

    private void Cargar(string periodo)
    {
        periodoActual = periodo;
        var hasta = DateTime.Today;
        var desde = hasta.AddYears(-1);
        Resultado<List<IngresoReporte>> resultado;
        if (periodo == "Semanal")
            resultado = negocio.IngresosPorSemana(desde, hasta);
        else if (periodo == "Mensual")
            resultado = negocio.IngresosPorMes(desde, hasta);
        else
            resultado = negocio.IngresosPorDia(desde, hasta);

        if (!resultado.Exito)
        {
            MessageBox.Show(resultado.Mensaje, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        ingresosActuales = resultado.Valor;
        dgvIngresos.Rows.Clear();
        foreach (var ingreso in ingresosActuales)
            dgvIngresos.Rows.Add(ingreso.Periodo.ToString(periodo == "Diario" ? "dd/MM/yyyy" : "MM/yyyy"), Tema.Dinero(ingreso.Total));

        var productos = negocio.ProductosMasVendidos();
        dgvProductosVendidos.Rows.Clear();
        if (productos.Exito)
            foreach (var producto in productos.Valor.Take(10))
                dgvProductosVendidos.Rows.Add(producto.Nombre, producto.Cantidad, Tema.Dinero(producto.Total));

        lblTotal.Text = Tema.Dinero(ingresosActuales.Sum(x => x.Total));
        lblRegistros.Text = ingresosActuales.Count.ToString();
        lblProductos.Text = productos.Exito ? productos.Valor.Sum(x => x.Cantidad).ToString() : "0";
    }

    private void Exportar(object sender, EventArgs e)
    {
        using (var dialogo = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "reporte_" + periodoActual.ToLowerInvariant() + ".csv" })
        {
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            var resultado = negocio.ExportarIngresosCsv(ingresosActuales, dialogo.FileName);
            MessageBox.Show(resultado.Mensaje, "Reportes", MessageBoxButtons.OK,
                resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}

/// <summary>Administración de usuarios activos y sus roles.</summary>
internal sealed class UcUsuariosReal : UserControl
{
    private readonly UsuarioNegocio negocio = new UsuarioNegocio();
    private readonly DataGridView dgvUsuarios = Tema.NuevaGrilla();

    public UcUsuariosReal()
    {
        BackColor = Tema.Fondo;
        dgvUsuarios.Dock = DockStyle.Fill;
        foreach (var columna in new[] { "Id", "Nombre", "Usuario", "Rol", "Estado" })
            dgvUsuarios.Columns.Add(columna, columna);
        Controls.Add(dgvUsuarios);
        var pnlAcciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
        pnlAcciones.Controls.Add(VistaRealUtil.Boton("Nuevo usuario", true, Nuevo));
        pnlAcciones.Controls.Add(VistaRealUtil.Boton("Editar", false, Editar));
        pnlAcciones.Controls.Add(VistaRealUtil.Boton("Desactivar", false, Desactivar));
        Controls.Add(pnlAcciones);
        Cargar();
    }

    private void Cargar()
    {
        var resultado = negocio.Listar();
        if (!resultado.Exito)
        {
            MessageBox.Show(resultado.Mensaje, "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        dgvUsuarios.Rows.Clear();
        foreach (var usuario in resultado.Valor.OrderBy(x => x.Nombre))
        {
            var fila = dgvUsuarios.Rows[dgvUsuarios.Rows.Add(usuario.idUsuario, usuario.Nombre, usuario.NombreUsuario, usuario.Rol, "Activo")];
            fila.Tag = usuario;
        }
    }

    private Usuario Seleccionado()
    {
        return dgvUsuarios.CurrentRow == null ? null : dgvUsuarios.CurrentRow.Tag as Usuario;
    }

    private void Nuevo(object sender, EventArgs e)
    {
        using (var formulario = new FrmUsuarioModal(null))
        {
            if (formulario.ShowDialog(FindForm()) != DialogResult.OK) return;
            var resultado = negocio.Alta(formulario.Nombre, formulario.NombreUsuario, formulario.Contraseña, formulario.Rol);
            Mostrar(resultado);
            if (resultado.Exito) Cargar();
        }
    }

    private void Editar(object sender, EventArgs e)
    {
        var usuario = Seleccionado();
        if (usuario == null) { AvisarSeleccion(); return; }
        using (var formulario = new FrmUsuarioModal(usuario))
        {
            if (formulario.ShowDialog(FindForm()) != DialogResult.OK) return;
            var resultado = negocio.Editar(usuario.idUsuario, formulario.Nombre, formulario.NombreUsuario,
                formulario.Rol, formulario.Contraseña);
            Mostrar(resultado);
            if (resultado.Exito) Cargar();
        }
    }

    private void Desactivar(object sender, EventArgs e)
    {
        var usuario = Seleccionado();
        if (usuario == null) { AvisarSeleccion(); return; }
        if (MessageBox.Show("¿Querés desactivar a " + usuario.Nombre + "?", "Usuarios",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var resultado = negocio.Desactivar(usuario.idUsuario);
        Mostrar(resultado);
        if (resultado.Exito) Cargar();
    }

    private void AvisarSeleccion()
    {
        MessageBox.Show("Seleccioná un usuario.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void Mostrar(Resultado resultado)
    {
        MessageBox.Show(resultado.Mensaje, "Usuarios", MessageBoxButtons.OK,
            resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private static void Mostrar(Resultado<Usuario> resultado)
    {
        MessageBox.Show(resultado.Mensaje, "Usuarios", MessageBoxButtons.OK,
            resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}

/// <summary>Formulario modal para alta y edición de usuarios.</summary>
internal sealed class FrmUsuarioModal : Form
{
    private readonly TextBox txtNombre = Tema.Caja("Nombre");
    private readonly TextBox txtUsuario = Tema.Caja("Usuario");
    private readonly TextBox txtContraseña = Tema.Caja("Contraseña");
    private readonly ComboBox cmbRol = new ComboBox();
    private readonly bool editar;

    public int IdUsuario { get; private set; }
    public string Nombre { get { return txtNombre.Text.Trim(); } }
    public string NombreUsuario { get { return txtUsuario.Text.Trim(); } }
    public string Contraseña { get { return txtContraseña.Text; } }
    public string Rol { get { return cmbRol.SelectedItem == null ? string.Empty : cmbRol.SelectedItem.ToString(); } }

    public FrmUsuarioModal(Usuario usuario)
    {
        editar = usuario != null;
        IdUsuario = usuario == null ? 0 : usuario.idUsuario;
        Text = editar ? "Editar usuario" : "Nuevo usuario";
        ClientSize = new Size(450, 285);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Tema.Fondo;
        AgregarCampo("Nombre", txtNombre, 20);
        AgregarCampo("Usuario", txtUsuario, 68);
        AgregarCampo("Contraseña", txtContraseña, 116);
        var lblRol = Tema.Etiqueta("Rol", 9.5f, false, Tema.Gris);
        lblRol.SetBounds(20, 164, 150, 22);
        cmbRol.SetBounds(180, 161, 240, 28);
        cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbRol.Items.AddRange(new object[] { "Dueño", "Empleado", "Administrador" });
        Controls.Add(lblRol);
        Controls.Add(cmbRol);
        if (usuario != null)
        {
            txtNombre.Text = usuario.Nombre;
            txtUsuario.Text = usuario.NombreUsuario;
            cmbRol.SelectedItem = usuario.Rol;
        }
        else cmbRol.SelectedIndex = 1;

        var btnCancelar = Tema.Boton("Cancelar", false);
        btnCancelar.SetBounds(220, 220, 95, 40);
        btnCancelar.DialogResult = DialogResult.Cancel;
        Controls.Add(btnCancelar);
        var btnAceptar = Tema.Boton("Guardar");
        btnAceptar.SetBounds(325, 220, 95, 40);
        btnAceptar.Click += Aceptar;
        Controls.Add(btnAceptar);
        AcceptButton = btnAceptar;
        CancelButton = btnCancelar;
    }

    private void AgregarCampo(string texto, TextBox caja, int y)
    {
        var etiqueta = Tema.Etiqueta(texto, 9.5f, false, Tema.Gris);
        etiqueta.SetBounds(20, y, 150, 22);
        caja.SetBounds(180, y - 3, 240, 28);
        if (caja == txtContraseña) caja.UseSystemPasswordChar = true;
        Controls.Add(etiqueta);
        Controls.Add(caja);
    }

    private void Aceptar(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(NombreUsuario) || string.IsNullOrWhiteSpace(Rol))
        {
            MessageBox.Show("Nombre, usuario y rol son obligatorios.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrEmpty(Contraseña))
        {
            MessageBox.Show("La contraseña no puede quedar vacía.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>Operaciones de copia y restauración de los XML desde Negocio.</summary>
internal sealed class UcConfiguracionReal : UserControl
{
    private readonly CopiaSeguridadNegocio negocio = new CopiaSeguridadNegocio();

    public UcConfiguracionReal()
    {
        BackColor = Tema.Fondo;
        var pnlTarjeta = Tema.NuevaTarjeta();
        pnlTarjeta.SetBounds(0, 0, 620, 220);
        var lblTitulo = Tema.Etiqueta("Copia de seguridad", 12, true);
        lblTitulo.Location = new Point(20, 16);
        var lblDetalle = Tema.Etiqueta("Copia y restaura los archivos XML del sistema.", 9, false, Tema.Gris);
        lblDetalle.Location = new Point(20, 46);
        var btnCrearCopia = Tema.Boton("Crear copia ahora");
        btnCrearCopia.SetBounds(20, 90, 250, 42);
        btnCrearCopia.Click += CrearCopia;
        var btnRestaurarCopia = Tema.Boton("Restaurar copia", false);
        btnRestaurarCopia.SetBounds(285, 90, 250, 42);
        btnRestaurarCopia.Click += RestaurarCopia;
        pnlTarjeta.Controls.Add(lblTitulo);
        pnlTarjeta.Controls.Add(lblDetalle);
        pnlTarjeta.Controls.Add(btnCrearCopia);
        pnlTarjeta.Controls.Add(btnRestaurarCopia);
        Controls.Add(pnlTarjeta);
    }

    private void CrearCopia(object sender, EventArgs e)
    {
        using (var dialogo = new FolderBrowserDialog { Description = "Elegí dónde guardar la copia" })
        {
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK) return;
            var resultado = negocio.CrearCopia(dialogo.SelectedPath);
            MessageBox.Show(resultado.Mensaje, "Configuración", MessageBoxButtons.OK,
                resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }

    private void RestaurarCopia(object sender, EventArgs e)
    {
        using (var dialogo = new FolderBrowserDialog { Description = "Elegí la carpeta de la copia a restaurar" })
        {
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK) return;
            if (MessageBox.Show("La restauración reemplazará los XML actuales. ¿Continuar?", "Configuración",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            var resultado = negocio.RestaurarCopia(dialogo.SelectedPath);
            MessageBox.Show(resultado.Mensaje, "Configuración", MessageBoxButtons.OK,
                resultado.Exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}

public partial class FrmPrincipal
{
        // ---------------------------------------------------------- menú lateral
        private Panel ConstruirMenu()
        {
            var pnlMenu = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Tema.Panel };

            var lblLogo = Tema.Etiqueta("CAMINOS", 18, true, Tema.Verde);
            lblLogo.Location = new Point(16, 16);
            var lblSubtituloMenu = Tema.Etiqueta("ROPA DEPORTIVA", 8, false, Tema.Gris);
            lblSubtituloMenu.Location = new Point(18, 50);
            pnlMenu.Controls.Add(lblLogo);
            pnlMenu.Controls.Add(lblSubtituloMenu);

            int y = 90;
            foreach (var nombre in Secciones)
            {
                if (!Permitido(nombre)) continue;
                var btnSeccion = new Button
                {
                    Text = "   " + nombre,
                    Tag = nombre,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = Tema.Fuente(10.5f, true),
                    Cursor = Cursors.Hand,
                    Bounds = new Rectangle(10, y, 190, 42)
                };
                btnSeccion.FlatAppearance.BorderSize = 0;
                btnSeccion.Click += (s, e) => Navegar((string)((Button)s).Tag);
                botones.Add(btnSeccion);
                pnlMenu.Controls.Add(btnSeccion);
                y += 48;
            }

            var lblLema = Tema.Etiqueta("Más deporte, mejor vida", 8.5f, false, Tema.Verde);
            lblLema.Dock = DockStyle.Bottom;
            lblLema.AutoSize = false;
            lblLema.Height = 36;
            lblLema.TextAlign = ContentAlignment.MiddleCenter;

            var btnCerrarSesion = new Button
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
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            btnCerrarSesion.Click += (s, e) => CerrarSesion();

            pnlMenu.Controls.Add(lblLema);
            pnlMenu.Controls.Add(btnCerrarSesion);
            return pnlMenu;
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
            var pnlEncabezado = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.Panel };

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

            pnlEncabezado.Controls.Add(lblTitulo);
            pnlEncabezado.Controls.Add(lblSubtitulo);
            pnlEncabezado.Controls.Add(lblUsuario);
            pnlEncabezado.Controls.Add(lblFecha);
            return pnlEncabezado;
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
            return new UcInicioReal();
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
            return new UcReportesReal();
        }

        private Control VistaUsuarios()
        {
            return new UcUsuariosReal();
        }

        private Control VistaConfiguracion()
        {
            return new UcConfiguracionReal();
        }
    }
}
