using System;

namespace SistemaCaminosalDeporte.Entidades
{
    public class Venta
    {
        public int idVenta { get; set; }
        public DateTime Fecha { get; set; }
        public string MedioPago { get; set; }
        public decimal MontoTotal { get; set; }
        public int idUsuario { get; set; }
    }
}
