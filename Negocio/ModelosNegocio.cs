using System;

namespace SistemaCaminosalDeporte.Negocio
{
    public class LineaVenta
    {
        public int idProducto { get; set; }
        public int Cantidad { get; set; }
    }

    public class IngresoReporte
    {
        public DateTime Periodo { get; set; }
        public decimal Total { get; set; }
    }

    public class ProductoVendidoReporte
    {
        public int idProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}
