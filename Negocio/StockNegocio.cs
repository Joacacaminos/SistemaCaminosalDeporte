using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    /// <summary>Reglas de movimientos de entrada, salida y ajustes de stock.</summary>
    public class StockNegocio
    {
        public static readonly string[] MotivosEntrada =
        {
            "Compra o reposición",
            "Devolución de cliente",
            "Ajuste de inventario",
            "Eliminación de venta"
        };

        public static readonly string[] MotivosSalida =
        {
            "Venta",
            "Robo o pérdida",
            "Rotura o defecto",
            "Uso interno o muestra",
            "Ajuste de inventario"
        };

        private readonly ProductoDatos productos;
        private readonly MovimientoStockDatos movimientos;
        private readonly UsuarioDatos usuarios;

        public StockNegocio()
            : this(new ProductoDatos(), new MovimientoStockDatos(), new UsuarioDatos())
        {
        }

        public StockNegocio(ProductoDatos productos, MovimientoStockDatos movimientos, UsuarioDatos usuarios)
        {
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
            this.movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
            this.usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        }

        public static List<string> ObtenerMotivosEntrada()
        {
            return MotivosEntrada.ToList();
        }

        public static List<string> ObtenerMotivosSalida()
        {
            return MotivosSalida.ToList();
        }

        /// <summary>Registra una entrada y aumenta el stock del producto.</summary>
        public Resultado<Producto> IngresarMercaderia(int idProducto, int cantidad, int idUsuario)
        {
            return IngresarMercaderia(idProducto, cantidad, idUsuario, "Compra o reposición", null);
        }

        /// <summary>Registra una entrada y aumenta el stock del producto.</summary>
        public Resultado<Producto> IngresarMercaderia(
            int idProducto, int cantidad, int idUsuario, string motivo, string observacion)
        {
            var permiso = ValidarOperacion(idUsuario);
            if (!permiso.Exito)
                return Resultado<Producto>.Error(permiso.Mensaje);
            if (cantidad <= 0)
                return Resultado<Producto>.Error("La cantidad de ingreso debe ser mayor a cero.");
            if (!MotivosEntrada.Contains(motivo))
                return Resultado<Producto>.Error("El motivo de entrada seleccionado no es válido.");
            var observacionValida = ValidarObservacion(motivo, observacion);
            if (!observacionValida.Exito)
                return Resultado<Producto>.Error(observacionValida.Mensaje);

            return AplicarMovimiento(idProducto, cantidad, "Entrada", motivo, observacion, null,
                "Ingreso de mercadería registrado correctamente.");
        }

        /// <summary>Registra una salida y disminuye el stock del producto.</summary>
        public Resultado<Producto> RegistrarSalida(
            int idProducto, int cantidad, int idUsuario, string motivo, string observacion)
        {
            var permiso = ValidarOperacion(idUsuario);
            if (!permiso.Exito)
                return Resultado<Producto>.Error(permiso.Mensaje);
            if (cantidad <= 0)
                return Resultado<Producto>.Error("La cantidad de salida debe ser mayor a cero.");
            if (!MotivosSalida.Contains(motivo))
                return Resultado<Producto>.Error("El motivo de salida seleccionado no es válido.");
            var observacionValida = ValidarObservacion(motivo, observacion);
            if (!observacionValida.Exito)
                return Resultado<Producto>.Error(observacionValida.Mensaje);

            var producto = productos.ObtenerPorId(idProducto);
            if (producto == null || !producto.Activo)
                return Resultado<Producto>.Error("No se encontró un producto activo con ese ID.");
            if (producto.Stock < cantidad)
                return Resultado<Producto>.Error("No se puede retirar más stock del disponible.");

            return AplicarMovimiento(idProducto, -cantidad, "Salida", motivo, observacion, null,
                "Salida de stock registrada correctamente.");
        }

        /// <summary>Ajusta el stock y registra la diferencia como movimiento.</summary>
        public Resultado<Producto> AjustarCantidad(int idProducto, int nuevaCantidad, int idUsuario)
        {
            return AjustarCantidad(idProducto, nuevaCantidad, idUsuario, "Ajuste de inventario", null);
        }

        /// <summary>Ajusta el stock y registra la diferencia como movimiento.</summary>
        public Resultado<Producto> AjustarCantidad(
            int idProducto, int nuevaCantidad, int idUsuario, string motivo, string observacion)
        {
            var permiso = ValidarOperacion(idUsuario);
            if (!permiso.Exito)
                return Resultado<Producto>.Error(permiso.Mensaje);
            if (nuevaCantidad < 0)
                return Resultado<Producto>.Error("El stock no puede ser negativo.");
            if (motivo != "Ajuste de inventario")
                return Resultado<Producto>.Error("El ajuste de cantidad debe usar el motivo Ajuste de inventario.");
            if (string.IsNullOrWhiteSpace(observacion))
                return Resultado<Producto>.Error("La observación es obligatoria para un ajuste de inventario.");

            var producto = productos.ObtenerPorId(idProducto);
            if (producto == null || !producto.Activo)
                return Resultado<Producto>.Error("No se encontró un producto activo con ese ID.");

            var diferencia = nuevaCantidad - producto.Stock;
            if (diferencia == 0)
                return Resultado<Producto>.Ok(producto, "El stock ya tenía la cantidad indicada.");

            return AplicarMovimiento(idProducto, diferencia, diferencia > 0 ? "Entrada" : "Salida",
                motivo, observacion, null, "Stock ajustado correctamente.");
        }

        public Resultado<List<Producto>> ListarStockBajo(int limite)
        {
            if (limite < 0)
                return Resultado<List<Producto>>.Error("El límite de stock no puede ser negativo.");

            try
            {
                var resultado = productos.ObtenerTodos()
                    .Where(x => x.Activo && x.Stock <= limite)
                    .OrderBy(x => x.Stock)
                    .ToList();
                return Resultado<List<Producto>>.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Resultado<List<Producto>>.Error("No se pudo consultar el stock bajo: " + ex.Message);
            }
        }

        public Resultado<List<MovimientoStockHistorial>> Historial(
            string tipo, string motivo, DateTime? desde, DateTime? hasta)
        {
            if (!SesionActual.EsDueno)
                return Resultado<List<MovimientoStockHistorial>>.Error("Solo el Dueño puede consultar el historial de movimientos.");
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                return Resultado<List<MovimientoStockHistorial>>.Error("La fecha inicial no puede ser posterior a la fecha final.");
            if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Entrada" && tipo != "Salida")
                return Resultado<List<MovimientoStockHistorial>>.Error("El filtro de tipo no es válido.");

            try
            {
                var productosPorId = productos.ObtenerTodos().ToDictionary(x => x.idProducto);
                var usuariosPorId = usuarios.ObtenerTodos().ToDictionary(x => x.idUsuario);
                var movimientosFiltrados = movimientos.ObtenerTodos()
                    .Where(x => string.IsNullOrWhiteSpace(tipo) || x.Tipo == tipo)
                    .Where(x => string.IsNullOrWhiteSpace(motivo) || ObtenerMotivoVisible(x) == motivo)
                    .Where(x => !desde.HasValue || x.Fecha.Date >= desde.Value.Date)
                    .Where(x => !hasta.HasValue || x.Fecha.Date <= hasta.Value.Date)
                    .OrderByDescending(x => x.Fecha)
                    .Select(x => CrearHistorial(x, productosPorId, usuariosPorId))
                    .ToList();
                return Resultado<List<MovimientoStockHistorial>>.Ok(movimientosFiltrados);
            }
            catch (Exception ex)
            {
                return Resultado<List<MovimientoStockHistorial>>.Error("No se pudo cargar el historial: " + ex.Message);
            }
        }

        public Resultado ExportarHistorialCsv(IEnumerable<MovimientoStockHistorial> historial, string rutaArchivo)
        {
            if (!SesionActual.EsDueno)
                return Resultado.Error("Solo el Dueño puede exportar el historial de movimientos.");
            if (historial == null)
                return Resultado.Error("No hay movimientos para exportar.");
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                return Resultado.Error("La ruta del archivo es obligatoria.");

            try
            {
                var lineas = new List<string>
                {
                    "Fecha;Producto;Talle;Tipo;Cantidad;Motivo;Observación;Usuario;Número de venta"
                };
                lineas.AddRange(historial.Select(x => string.Join(";", new[]
                {
                    Csv(x.Fecha.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                    Csv(x.Producto), Csv(x.Talle), Csv(x.Tipo), x.Cantidad.ToString(CultureInfo.InvariantCulture),
                    Csv(x.Motivo), Csv(x.Observacion), Csv(x.Usuario), Csv(x.NumeroVenta)
                })));
                File.WriteAllLines(rutaArchivo, lineas, Encoding.UTF8);
                return Resultado.Ok("Historial exportado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudo exportar el historial: " + ex.Message);
            }
        }

        private Resultado<Producto> AplicarMovimiento(
            int idProducto, int variacion, string tipo, string motivo, string observacion,
            int? idVenta, string mensajeExito)
        {
            try
            {
                var producto = productos.ObtenerPorId(idProducto);
                if (producto == null || !producto.Activo)
                    return Resultado<Producto>.Error("No se encontró un producto activo con ese ID.");
                if (producto.Stock + variacion < 0)
                    return Resultado<Producto>.Error("El stock no puede quedar negativo.");

                var productosOriginales = productos.ObtenerTodos().Select(ClonarProducto).ToList();
                var movimientosOriginales = movimientos.ObtenerTodos().Select(ClonarMovimiento).ToList();
                producto.Stock += variacion;
                productos.Guardar();
                movimientos.Agregar(new MovimientoStock
                {
                    idProducto = idProducto,
                    Tipo = tipo,
                    Cantidad = Math.Abs(variacion),
                    Fecha = DateTime.Now,
                    idUsuario = SesionActual.Usuario.idUsuario,
                    Motivo = motivo,
                    Observacion = observacion,
                    IdVenta = idVenta
                });
                return Resultado<Producto>.Ok(producto, mensajeExito);
            }
            catch (Exception ex)
            {
                return Resultado<Producto>.Error("No se pudo guardar el movimiento: " + ex.Message);
            }
        }

        private Resultado<Usuario> ValidarOperacion(int idUsuario)
        {
            if (!SesionActual.EsDueno)
                return Resultado<Usuario>.Error("Solo el Dueño puede modificar movimientos de stock.");
            if (SesionActual.Usuario == null || SesionActual.Usuario.idUsuario != idUsuario)
                return Resultado<Usuario>.Error("El usuario no coincide con la sesión activa.");
            var usuario = usuarios.ObtenerPorId(idUsuario);
            return usuario == null || !usuario.Activo
                ? Resultado<Usuario>.Error("El usuario que realiza la operación no existe o está inactivo.")
                : Resultado<Usuario>.Ok(usuario);
        }

        private static Resultado ValidarObservacion(string motivo, string observacion)
        {
            if (motivo == "Robo o pérdida" || motivo == "Rotura o defecto" || motivo == "Ajuste de inventario")
                return string.IsNullOrWhiteSpace(observacion)
                    ? Resultado.Error("La observación es obligatoria para el motivo seleccionado.")
                    : Resultado.Ok();
            return Resultado.Ok();
        }

        private static string ObtenerMotivoVisible(MovimientoStock movimiento)
        {
            return string.IsNullOrWhiteSpace(movimiento.Motivo) ? "Sin motivo registrado" : movimiento.Motivo;
        }

        private static MovimientoStockHistorial CrearHistorial(
            MovimientoStock movimiento, Dictionary<int, Producto> productosPorId,
            Dictionary<int, Usuario> usuariosPorId)
        {
            Producto producto;
            Usuario usuario;
            productosPorId.TryGetValue(movimiento.idProducto, out producto);
            usuariosPorId.TryGetValue(movimiento.idUsuario, out usuario);
            return new MovimientoStockHistorial
            {
                Fecha = movimiento.Fecha,
                Producto = producto == null ? "Producto eliminado" : producto.Nombre,
                Talle = producto == null ? string.Empty : producto.Talle,
                Tipo = movimiento.Tipo,
                Cantidad = movimiento.Cantidad,
                Motivo = ObtenerMotivoVisible(movimiento),
                Observacion = movimiento.Observacion ?? string.Empty,
                Usuario = usuario == null ? "Usuario desconocido" : usuario.NombreUsuario,
                NumeroVenta = movimiento.IdVenta.HasValue ? movimiento.IdVenta.Value.ToString() : "-"
            };
        }

        private static string Csv(string valor)
        {
            var texto = valor ?? string.Empty;
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }

        private static Producto ClonarProducto(Producto x)
        {
            return new Producto { idProducto = x.idProducto, Nombre = x.Nombre, Tipo = x.Tipo,
                Talle = x.Talle, Color = x.Color, Precio = x.Precio, Stock = x.Stock, Activo = x.Activo };
        }

        private static MovimientoStock ClonarMovimiento(MovimientoStock x)
        {
            return new MovimientoStock { idMovimiento = x.idMovimiento, idProducto = x.idProducto,
                Tipo = x.Tipo, Cantidad = x.Cantidad, Fecha = x.Fecha, idUsuario = x.idUsuario,
                Motivo = x.Motivo, Observacion = x.Observacion, IdVenta = x.IdVenta };
        }
    }
}
