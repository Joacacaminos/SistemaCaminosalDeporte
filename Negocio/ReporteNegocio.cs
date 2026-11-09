using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    public class ReporteNegocio
    {
        private readonly VentaDatos ventas;
        private readonly DetalleVentaDatos detalles;
        private readonly ProductoDatos productos;

        public ReporteNegocio()
            : this(new VentaDatos(), new DetalleVentaDatos(), new ProductoDatos())
        {
        }

        public Resultado ExportarIngresosCsv(IEnumerable<IngresoReporte> ingresos, string rutaArchivo)
        {
            if (ingresos == null)
                return Resultado.Error("No hay datos de ingresos para exportar.");
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                return Resultado.Error("La ruta del archivo es obligatoria.");

            try
            {
                var lineas = new List<string> { "Periodo;Total" };
                lineas.AddRange(ingresos.Select(x =>
                    x.Periodo.ToString("dd/MM/yyyy") + ";" + x.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                File.WriteAllLines(rutaArchivo, lineas, Encoding.UTF8);
                return Resultado.Ok("Reporte exportado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudo exportar el reporte: " + ex.Message);
            }
        }

        public ReporteNegocio(VentaDatos ventas, DetalleVentaDatos detalles, ProductoDatos productos)
        {
            this.ventas = ventas ?? throw new ArgumentNullException(nameof(ventas));
            this.detalles = detalles ?? throw new ArgumentNullException(nameof(detalles));
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
        }

        public Resultado<List<IngresoReporte>> IngresosPorDia(DateTime desde, DateTime hasta)
        {
            return AgruparIngresos(desde, hasta, venta => venta.Fecha.Date, "día");
        }

        public Resultado<List<IngresoReporte>> IngresosPorSemana(DateTime desde, DateTime hasta)
        {
            return AgruparIngresos(desde, hasta, venta => InicioDeSemana(venta.Fecha), "semana");
        }

        public Resultado<List<IngresoReporte>> IngresosPorMes(DateTime desde, DateTime hasta)
        {
            return AgruparIngresos(desde, hasta, venta => new DateTime(venta.Fecha.Year, venta.Fecha.Month, 1), "mes");
        }

        public Resultado<List<ProductoVendidoReporte>> ProductosMasVendidos()
        {
            try
            {
                var nombres = productos.ObtenerTodos().ToDictionary(x => x.idProducto, x => x.Nombre);
                var resultado = detalles.ObtenerTodos()
                    .GroupBy(x => x.idProducto)
                    .Select(grupo => new ProductoVendidoReporte
                    {
                        idProducto = grupo.Key,
                        Nombre = nombres.ContainsKey(grupo.Key) ? nombres[grupo.Key] : "Producto eliminado",
                        Cantidad = grupo.Sum(x => x.Cantidad),
                        Total = grupo.Sum(x => x.Cantidad * x.PrecioUnitario)
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .ThenByDescending(x => x.Total)
                    .ToList();
                return Resultado<List<ProductoVendidoReporte>>.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Resultado<List<ProductoVendidoReporte>>.Error("No se pudo generar el reporte de productos más vendidos: " + ex.Message);
            }
        }

        private Resultado<List<IngresoReporte>> AgruparIngresos(
            DateTime desde,
            DateTime hasta,
            Func<Venta, DateTime> agrupador,
            string nombrePeriodo)
        {
            if (desde.Date > hasta.Date)
                return Resultado<List<IngresoReporte>>.Error("La fecha inicial no puede ser posterior a la fecha final.");

            try
            {
                var inicio = desde.Date;
                var finExclusivo = hasta.Date.AddDays(1);
                var resultado = ventas.ObtenerTodos()
                    .Where(x => x.Fecha >= inicio && x.Fecha < finExclusivo)
                    .GroupBy(agrupador)
                    .Select(grupo => new IngresoReporte
                    {
                        Periodo = grupo.Key,
                        Total = grupo.Sum(x => x.MontoTotal)
                    })
                    .OrderBy(x => x.Periodo)
                    .ToList();
                return Resultado<List<IngresoReporte>>.Ok(resultado, "Reporte de ingresos por " + nombrePeriodo + " generado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<List<IngresoReporte>>.Error("No se pudo generar el reporte de ingresos por " + nombrePeriodo + ": " + ex.Message);
            }
        }

        private static DateTime InicioDeSemana(DateTime fecha)
        {
            var diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
            return fecha.Date.AddDays(-diasDesdeLunes);
        }
    }
}
