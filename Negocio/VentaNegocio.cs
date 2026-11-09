using System;
using System.Collections.Generic;
using System.Linq;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    /// <summary>Valida y registra ventas, detalles y movimientos de salida.</summary>
    public class VentaNegocio
    {
        public const decimal RecargoTarjetaPorcentaje = 0.15m;

        public PagoCalculado CalcularPago(decimal subtotal, string medioPago, string cuotaPlan, decimal porcentajeDescuento)
        {
            var descuento = subtotal * porcentajeDescuento / 100m;
            var baseParaRecargo = subtotal - descuento;
            var recargo = medioPago == "Tarjeta" ? baseParaRecargo * RecargoTarjetaPorcentaje : 0m;
            return new PagoCalculado
            {
                Subtotal = subtotal,
                Descuento = descuento,
                Recargo = recargo,
                Total = baseParaRecargo + recargo
            };
        }

        public Resultado<List<UltimaVenta>> VentasDelPeriodo(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
                return Resultado<List<UltimaVenta>>.Error("La fecha inicial no puede ser posterior a la fecha final.");

            try
            {
                var nombres = productos.ObtenerTodos().ToDictionary(x => x.idProducto, x => x.Nombre);
                var detallesPorVenta = detalles.ObtenerTodos().GroupBy(x => x.idVenta)
                    .ToDictionary(x => x.Key, x => x.ToList());
                var finExclusivo = hasta.Date.AddDays(1);
                var resultado = ventas.ObtenerTodos()
                    .Where(x => x.Fecha >= desde.Date && x.Fecha < finExclusivo)
                    .OrderByDescending(x => x.Fecha)
                    .Select(venta => CrearUltimaVenta(venta, nombres, detallesPorVenta))
                    .ToList();
                return Resultado<List<UltimaVenta>>.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Resultado<List<UltimaVenta>>.Error("No se pudieron cargar las ventas del período: " + ex.Message);
            }
        }

        public Resultado EliminarVenta(int idVenta, Usuario usuario)
        {
            if (idVenta <= 0)
                return Resultado.Error("La venta seleccionada no es válida.");
            if (usuario == null || (usuario.Rol != "Dueño" && usuario.Rol != "Administrador"))
                return Resultado.Error("Solo los roles Dueño y Administrador pueden eliminar ventas.");

            var usuarioActual = usuarios.ObtenerPorId(usuario.idUsuario);
            if (usuarioActual == null || !usuarioActual.Activo)
                return Resultado.Error("El usuario que elimina la venta no existe o está inactivo.");

            var ventasOriginales = ventas.ObtenerTodos().Select(ClonarVenta).ToList();
            var detallesOriginales = detalles.ObtenerTodos().Select(ClonarDetalle).ToList();
            var productosOriginales = productos.ObtenerTodos().Select(ClonarProducto).ToList();
            var movimientosOriginales = movimientos.ObtenerTodos().Select(ClonarMovimiento).ToList();

            try
            {
                var venta = ventasOriginales.FirstOrDefault(x => x.idVenta == idVenta);
                if (venta == null)
                    return Resultado.Error("La venta seleccionada no existe.");

                var detallesVenta = detallesOriginales.Where(x => x.idVenta == idVenta).ToList();

                var productosActuales = productos.ObtenerTodos();
                foreach (var detalle in detallesVenta)
                {
                    var producto = productosActuales.FirstOrDefault(x => x.idProducto == detalle.idProducto);
                    if (producto == null)
                        return Resultado.Error("No se puede eliminar la venta porque falta el producto " + detalle.idProducto + ".");
                    producto.Stock += detalle.Cantidad;
                }

                ventas.ReemplazarTodos(ventasOriginales.Where(x => x.idVenta != idVenta));
                detalles.ReemplazarTodos(detallesOriginales.Where(x => x.idVenta != idVenta));
                productos.Guardar();
                foreach (var detalle in detallesVenta)
                {
                    movimientos.Agregar(new MovimientoStock
                    {
                        idProducto = detalle.idProducto,
                        Tipo = "Entrada - Eliminación de venta",
                        Cantidad = detalle.Cantidad,
                        Fecha = DateTime.Now,
                        idUsuario = usuario.idUsuario
                    });
                }

                return Resultado.Ok("Venta eliminada correctamente y stock restaurado.");
            }
            catch (Exception ex)
            {
                var restaurado = Restaurar(ventasOriginales, detallesOriginales, productosOriginales, movimientosOriginales);
                var mensaje = "No se pudo eliminar la venta: " + ex.Message;
                if (!restaurado)
                    mensaje += " Además, no se pudo restaurar completamente el estado anterior.";
                return Resultado.Error(mensaje);
            }
        }

        private static UltimaVenta CrearUltimaVenta(Venta venta, Dictionary<int, string> nombres,
            Dictionary<int, List<DetalleVenta>> detallesPorVenta)
        {
            List<DetalleVenta> detallesVenta;
            if (!detallesPorVenta.TryGetValue(venta.idVenta, out detallesVenta))
                detallesVenta = new List<DetalleVenta>();
            var nombresProductos = detallesVenta.Select(x => nombres.ContainsKey(x.idProducto)
                ? nombres[x.idProducto] : "Producto eliminado").ToList();
            return new UltimaVenta
            {
                Fecha = venta.Fecha,
                idVenta = venta.idVenta,
                Producto = nombresProductos.Count == 0 ? "Sin detalle" : nombresProductos[0]
                    + (nombresProductos.Count > 1 ? " y otros" : string.Empty),
                Cantidad = detallesVenta.Sum(x => x.Cantidad),
                Total = venta.MontoTotal,
                MedioPago = ObtenerDescripcionMedioPago(venta),
                CuotaPlan = venta.CuotaPlan
            };
        }

        private readonly ProductoDatos productos;
        private readonly VentaDatos ventas;
        private readonly DetalleVentaDatos detalles;
        private readonly MovimientoStockDatos movimientos;
        private readonly UsuarioDatos usuarios;

        public VentaNegocio()
            : this(
                new ProductoDatos(),
                new VentaDatos(),
                new DetalleVentaDatos(),
                new MovimientoStockDatos(),
                new UsuarioDatos())
        {
        }

        public VentaNegocio(
            ProductoDatos productos,
            VentaDatos ventas,
            DetalleVentaDatos detalles,
            MovimientoStockDatos movimientos,
            UsuarioDatos usuarios)
        {
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
            this.ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            this.detalles = detalles ?? throw new ArgumentNullException(nameof(detalles));
            this.movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
            this.usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        }

        /// <summary>Registra una venta únicamente si usuario, productos y stock son válidos.</summary>
        public Resultado<Venta> Registrar(
            IEnumerable<LineaVenta> lineas,
            string medioPago,
            Usuario usuario)
        {
            return Registrar(lineas, medioPago, CuotaPlanPredeterminada(medioPago), 0m, usuario);
        }

        /// <summary>Registra la venta calculando descuento, recargo y total final.</summary>
        public Resultado<Venta> Registrar(
            IEnumerable<LineaVenta> lineas,
            string medioPago,
            string cuotaPlan,
            decimal porcentajeDescuento,
            Usuario usuario)
        {
            var listaLineas = lineas == null ? null : lineas.ToList();
            var validacion = ValidarDatos(listaLineas, medioPago, cuotaPlan, porcentajeDescuento, usuario);
            if (!validacion.Exito)
                return Resultado<Venta>.Error(validacion.Mensaje);

            var lineasNormalizadas = listaLineas
                .GroupBy(x => x.idProducto)
                .Select(x => new LineaVenta
                {
                    idProducto = x.Key,
                    Cantidad = x.Sum(y => y.Cantidad)
                })
                .ToList();

            var productosOriginales = productos.ObtenerTodos().Select(ClonarProducto).ToList();
            var productosActuales = productos.ObtenerTodos();
            var ventaActuales = ventas.ObtenerTodos();
            var detallesActuales = detalles.ObtenerTodos();
            var movimientosActuales = movimientos.ObtenerTodos();

            try
            {
                foreach (var linea in lineasNormalizadas)
                {
                    var producto = productosActuales.FirstOrDefault(x => x.idProducto == linea.idProducto);
                    if (producto == null || !producto.Activo)
                        return Resultado<Venta>.Error("Uno de los productos no existe o está inactivo.");
                    if (producto.Stock < linea.Cantidad)
                        return Resultado<Venta>.Error("No hay stock suficiente para el producto: " + producto.Nombre + ".");
                }

                var subtotal = lineasNormalizadas.Sum(linea =>
                {
                    var producto = productosActuales.First(x => x.idProducto == linea.idProducto);
                    return producto.Precio * linea.Cantidad;
                });
                var pago = CalcularPago(subtotal, medioPago, cuotaPlan, porcentajeDescuento);

                var venta = new Venta
                {
                    Fecha = DateTime.Now,
                    MedioPago = medioPago,
                    CuotaPlan = cuotaPlan,
                    MontoSubtotal = subtotal,
                    MontoDescuento = pago.Descuento,
                    MontoRecargo = pago.Recargo,
                    MontoTotal = pago.Total,
                    idUsuario = usuario.idUsuario
                };
                ventas.Agregar(venta);

                var nuevosDetalles = lineasNormalizadas.Select(linea =>
                {
                    var producto = productosActuales.First(x => x.idProducto == linea.idProducto);
                    return new DetalleVenta
                    {
                        idVenta = venta.idVenta,
                        idProducto = producto.idProducto,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = producto.Precio
                    };
                }).ToList();
                detalles.ReemplazarTodos(detallesActuales.Concat(nuevosDetalles));

                foreach (var linea in lineasNormalizadas)
                {
                    var producto = productosActuales.First(x => x.idProducto == linea.idProducto);
                    producto.Stock -= linea.Cantidad;
                }
                productos.Guardar();

                foreach (var linea in lineasNormalizadas)
                {
                    movimientos.Agregar(new MovimientoStock
                    {
                        idProducto = linea.idProducto,
                        Tipo = "Salida",
                        Cantidad = linea.Cantidad,
                        Fecha = venta.Fecha,
                        idUsuario = usuario.idUsuario
                    });
                }

                return Resultado<Venta>.Ok(venta, "Venta registrada correctamente.");
            }
            catch (Exception ex)
            {
                var restaurado = Restaurar(ventaActuales, detallesActuales, productosOriginales, movimientosActuales);
                var mensaje = "No se pudo registrar la venta: " + ex.Message;
                if (!restaurado)
                    mensaje += " Además, no se pudieron restaurar todos los datos automáticamente.";
                return Resultado<Venta>.Error(mensaje);
            }
        }

        public Resultado<List<UltimaVenta>> UltimasVentas(int cantidad = 10)
        {
            if (cantidad <= 0)
                return Resultado<List<UltimaVenta>>.Error("La cantidad de ventas debe ser mayor a cero.");

            try
            {
                var nombres = productos.ObtenerTodos()
                    .ToDictionary(x => x.idProducto, x => x.Nombre);
                var detallesPorVenta = detalles.ObtenerTodos()
                    .GroupBy(x => x.idVenta)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var resultado = ventas.ObtenerTodos()
                    .OrderByDescending(x => x.Fecha)
                    .Take(cantidad)
                    .Select(venta =>
                    {
                        List<DetalleVenta> detallesVenta;
                        if (!detallesPorVenta.TryGetValue(venta.idVenta, out detallesVenta))
                            detallesVenta = new List<DetalleVenta>();

                        var nombresProductos = detallesVenta
                            .Select(x => nombres.ContainsKey(x.idProducto) ? nombres[x.idProducto] : "Producto eliminado")
                            .ToList();

                        return new UltimaVenta
                        {
                            Fecha = venta.Fecha,
                            idVenta = venta.idVenta,
                            Producto = nombresProductos.Count == 0
                                ? "Sin detalle"
                                : nombresProductos[0] + (nombresProductos.Count > 1 ? " y otros" : string.Empty),
                            Cantidad = detallesVenta.Sum(x => x.Cantidad),
                            Total = venta.MontoTotal,
                            MedioPago = ObtenerDescripcionMedioPago(venta),
                            CuotaPlan = venta.CuotaPlan
                        };
                    })
                    .ToList();

                return Resultado<List<UltimaVenta>>.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Resultado<List<UltimaVenta>>.Error("No se pudieron cargar las últimas ventas: " + ex.Message);
            }
        }

        private Resultado ValidarDatos(IEnumerable<LineaVenta> lineas, string medioPago, string cuotaPlan,
            decimal porcentajeDescuento, Usuario usuario)
        {
            try
            {
                if (lineas == null)
                    return Resultado.Error("Debe indicar al menos un producto.");

                var lista = lineas.ToList();
                if (lista.Count == 0)
                    return Resultado.Error("Debe indicar al menos un producto.");
                if (lista.Any(x => x == null || x.idProducto <= 0 || x.Cantidad <= 0))
                    return Resultado.Error("Cada producto debe tener un ID válido y una cantidad mayor a cero.");
                if (!MedioPagoValido(medioPago))
                    return Resultado.Error("El medio de pago debe ser Efectivo, Tarjeta, QR o Go Cuotas.");
                if (!CuotaPlanValido(medioPago, cuotaPlan))
                    return Resultado.Error("La cuota o plan seleccionado no es válido para el medio de pago.");
                if (porcentajeDescuento < 0 || porcentajeDescuento > 100)
                    return Resultado.Error("El porcentaje de descuento debe estar entre 0 y 100.");
                if (usuario == null || usuario.idUsuario <= 0 || !usuario.Activo)
                    return Resultado.Error("El usuario que registra la venta no es válido o está inactivo.");

                var usuarioActual = usuarios.ObtenerPorId(usuario.idUsuario);
                if (usuarioActual == null || !usuarioActual.Activo)
                    return Resultado.Error("El usuario que registra la venta no existe o está inactivo.");
                return Resultado.Ok();
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudieron validar los datos de la venta: " + ex.Message);
            }
        }

        private static bool CuotaPlanValido(string medioPago, string cuotaPlan)
        {
            if (medioPago == "Tarjeta")
                return cuotaPlan == "1 cuota" || cuotaPlan == "3 cuotas" || cuotaPlan == "Plan Z (Tarjeta Naranja)";
            if (medioPago == "Go Cuotas")
                return cuotaPlan == "1 cuota" || cuotaPlan == "3 cuotas";
            return string.IsNullOrEmpty(cuotaPlan);
        }

        private static string CuotaPlanPredeterminada(string medioPago)
        {
            return medioPago == "Tarjeta" || medioPago == "Go Cuotas" ? "1 cuota" : null;
        }

        public static string ObtenerDescripcionMedioPago(Venta venta)
        {
            if (venta == null || string.IsNullOrEmpty(venta.MedioPago))
                return "Medio no informado";
            return string.IsNullOrEmpty(venta.CuotaPlan)
                ? venta.MedioPago
                : venta.MedioPago + " - " + venta.CuotaPlan;
        }
        private bool Restaurar(
            List<Venta> ventasOriginales,
            List<DetalleVenta> detallesOriginales,
            List<Producto> productosOriginales,
            List<MovimientoStock> movimientosOriginales)
        {
            var restaurado = true;
            try
            {
                ventas.ReemplazarTodos(ventasOriginales.Select(ClonarVenta));
            }
            catch
            {
                restaurado = false;
            }
            try
            {
                detalles.ReemplazarTodos(detallesOriginales.Select(ClonarDetalle));
            }
            catch
            {
                restaurado = false;
            }
            try
            {
                productos.ReemplazarTodos(productosOriginales.Select(ClonarProducto));
            }
            catch
            {
                restaurado = false;
            }
            try
            {
                movimientos.ReemplazarTodos(movimientosOriginales.Select(ClonarMovimiento));
            }
            catch
            {
                restaurado = false;
            }
            return restaurado;
        }

        private static bool MedioPagoValido(string medioPago)
        {
            return medioPago == "Efectivo" || medioPago == "Tarjeta" || medioPago == "QR" || medioPago == "Go Cuotas";
        }

        private static Venta ClonarVenta(Venta x)
        {
            return new Venta
            {
                idVenta = x.idVenta,
                Fecha = x.Fecha,
                MedioPago = x.MedioPago,
                CuotaPlan = x.CuotaPlan,
                MontoSubtotal = x.MontoSubtotal,
                MontoDescuento = x.MontoDescuento,
                MontoRecargo = x.MontoRecargo,
                MontoTotal = x.MontoTotal,
                idUsuario = x.idUsuario
            };
        }

        private static DetalleVenta ClonarDetalle(DetalleVenta x)
        {
            return new DetalleVenta { idVenta = x.idVenta, idProducto = x.idProducto, Cantidad = x.Cantidad, PrecioUnitario = x.PrecioUnitario };
        }

        private static Producto ClonarProducto(Producto x)
        {
            return new Producto { idProducto = x.idProducto, Nombre = x.Nombre, Tipo = x.Tipo, Talle = x.Talle, Color = x.Color, Precio = x.Precio, Stock = x.Stock, Activo = x.Activo };
        }

        private static MovimientoStock ClonarMovimiento(MovimientoStock x)
        {
            return new MovimientoStock { idMovimiento = x.idMovimiento, idProducto = x.idProducto, Tipo = x.Tipo, Cantidad = x.Cantidad, Fecha = x.Fecha, idUsuario = x.idUsuario };
        }
    }
}
