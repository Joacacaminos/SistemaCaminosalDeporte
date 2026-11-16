using System;

namespace SistemaCaminosalDeporte.Entidades
{
    public class MovimientoStock
    {
        public int idMovimiento { get; set; }
        public int idProducto { get; set; }
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
        public int idUsuario { get; set; }
        public string Motivo { get; set; }
        public string Observacion { get; set; }
        public int? IdVenta { get; set; }
    }
}
