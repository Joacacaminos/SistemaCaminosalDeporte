using System;

namespace SistemaCaminosalDeporte.Negocio
{
    public class LineaVenta
    {
        public int idProducto { get; set; }
        public int Cantidad { get; set; }
    }

    public class PagoCalculado
    {
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Recargo { get; set; }
        public decimal Total { get; set; }
    }

    public class IngresoReporte
    {
        public DateTime Periodo { get; set; }
        public decimal Total { get; set; }
        public string MediosPago { get; set; }
    }

    public class ProductoVendidoReporte
    {
        public int idProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public class UltimaVenta
    {
        public DateTime Fecha { get; set; }
        public int idVenta { get; set; }
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public string MedioPago { get; set; }
        public string CuotaPlan { get; set; }
    }

    public class MovimientoStockHistorial
    {
        public DateTime Fecha { get; set; }
        public string Producto { get; set; }
        public string Talle { get; set; }
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public string Motivo { get; set; }
        public string Observacion { get; set; }
        public string Usuario { get; set; }
        public string NumeroVenta { get; set; }
    }
}
