using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using SistemaCaminosalDeporte.Negocio;

namespace SistemaCaminosalDeporte.Presentacion
{
    public class FrmPrincipal : Form  
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

        // ---------------------------------------------------------- vistas (datos de ejemplo)
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
            return new UcTabla(
                null,
                new[] { "Nuevo producto", "Editar", "Eliminar" },
                new[] { "Id", "Nombre", "Tipo", "Talle", "Color", "Precio", "Stock" },
                new[]
                {
                    new[] { "1", "Remera training", "Remera", "L", "Negro", "$ 15.990", "12" },
                    new[] { "2", "Short deportivo", "Short", "M", "Gris", "$ 12.990", "8" },
                    new[] { "3", "Pantalón deportivo", "Pantalón", "M", "Negro", "$ 12.990", "6" },
                    new[] { "4", "Buzo deportivo", "Buzo", "L", "Negro", "$ 22.990", "3" },
                    new[] { "5", "Zapatillas running", "Calzado", "42", "Negro", "$ 49.990", "5" },
                    new[] { "6", "Gorra deportiva", "Accesorio", "Único", "Negro", "$ 9.990", "10" }
                });
        }

        private Control VistaStock()
        {
            return new UcTabla(
                new[]
                {
                    new[] { "Prendas en stock", "184" },
                    new[] { "Productos con stock bajo", "4" },
                    new[] { "Sin stock", "1" }
                },
                new[] { "Registrar ingreso de mercadería", "Ajustar cantidad" },
                new[] { "Producto", "Talle", "Color", "Stock", "Estado" },
                new[]
                {
                    new[] { "Remera training", "L", "Negro", "12", "OK" },
                    new[] { "Short deportivo", "M", "Gris", "8", "OK" },
                    new[] { "Buzo deportivo", "L", "Negro", "3", "Bajo" },
                    new[] { "Zapatillas running", "42", "Negro", "5", "OK" },
                    new[] { "Botella deportiva", "Único", "Azul", "0", "Sin stock" }
                });
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
