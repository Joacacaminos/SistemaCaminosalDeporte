namespace SistemaCaminosalDeporte.Entidades
{
    public class DetalleVenta
    {
        public int idVenta { get; set; }
        public int idProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}
