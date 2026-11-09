using System;

namespace SistemaCaminosalDeporte.Entidades
{
    public class Venta
    {
        public int idVenta { get; set; }
        public DateTime Fecha { get; set; }
        public string MedioPago { get; set; }
        public string CuotaPlan { get; set; }
        public decimal MontoSubtotal { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal MontoRecargo { get; set; }
        public decimal MontoTotal { get; set; }
        public int idUsuario { get; set; }
    }
}
