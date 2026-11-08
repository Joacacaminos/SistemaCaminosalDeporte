using System.Collections.Generic;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    public class DetalleVentaDatos : RepositorioXml<DetalleVenta>
    {
        public DetalleVentaDatos()
            : base(
                "DetallesVenta.xml",
                detalle => detalle.idVenta,
                (detalle, id) => { },
                new List<DetalleVenta>())
        {
        }
    }
}
