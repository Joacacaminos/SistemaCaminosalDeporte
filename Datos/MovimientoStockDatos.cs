using System.Collections.Generic;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    public class MovimientoStockDatos : RepositorioXml<MovimientoStock>
    {
        public MovimientoStockDatos()
            : base(
                "MovimientosStock.xml",
                movimiento => movimiento.idMovimiento,
                (movimiento, id) => movimiento.idMovimiento = id,
                new List<MovimientoStock>())
        {
        }
    }
}
