using System.Collections.Generic;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    public class VentaDatos : RepositorioXml<Venta>
    {
        public VentaDatos()
            : base(
                "Ventas.xml",
                venta => venta.idVenta,
                (venta, id) => venta.idVenta = id,
                new List<Venta>())
        {
        }
    }
}
