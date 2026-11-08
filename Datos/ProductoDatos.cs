using System.Collections.Generic;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    public class ProductoDatos : RepositorioXml<Producto>
    {
        public ProductoDatos()
            : base(
                "Productos.xml",
                producto => producto.idProducto,
                (producto, id) => producto.idProducto = id,
                CrearProductosIniciales())
        {
        }

        private static IEnumerable<Producto> CrearProductosIniciales()
        {
            return new[]
            {
                new Producto { Nombre = "Remera Running Dry", Tipo = "Remera", Talle = "M", Color = "Negro", Precio = 18500m, Stock = 25, Activo = true },
                new Producto { Nombre = "Remera Training Pro", Tipo = "Remera", Talle = "L", Color = "Azul", Precio = 21000m, Stock = 18, Activo = true },
                new Producto { Nombre = "Calza Corta Active", Tipo = "Calza", Talle = "S", Color = "Gris", Precio = 19500m, Stock = 14, Activo = true },
                new Producto { Nombre = "Calza Larga Winter", Tipo = "Calza", Talle = "M", Color = "Negro", Precio = 28000m, Stock = 12, Activo = true },
                new Producto { Nombre = "Short Deportivo Flex", Tipo = "Short", Talle = "L", Color = "Bordo", Precio = 17500m, Stock = 20, Activo = true },
                new Producto { Nombre = "Buzo Hoodie Sport", Tipo = "Buzo", Talle = "XL", Color = "Verde", Precio = 42000m, Stock = 9, Activo = true },
                new Producto { Nombre = "Campera Rompeviento", Tipo = "Campera", Talle = "M", Color = "Azul", Precio = 52000m, Stock = 7, Activo = true },
                new Producto { Nombre = "Conjunto Deportivo", Tipo = "Conjunto", Talle = "L", Color = "Negro", Precio = 46500m, Stock = 11, Activo = true },
                new Producto { Nombre = "Top Deportivo Mujer", Tipo = "Top", Talle = "S", Color = "Rosa", Precio = 16000m, Stock = 16, Activo = true },
                new Producto { Nombre = "Medias Running Pack", Tipo = "Accesorio", Talle = "Único", Color = "Blanco", Precio = 8500m, Stock = 30, Activo = true }
            };
        }
    }
}
