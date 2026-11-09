using System.Collections.Generic;
using System.Linq;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    /// <summary>Repositorio XML de productos, incluyendo el catálogo inicial.</summary>
    public class ProductoDatos : RepositorioXml<Producto>
    {
        public ProductoDatos()
            : base(
                "Productos.xml",
                producto => producto.idProducto,
                (producto, id) => producto.idProducto = id,
                CrearProductosIniciales())
        {
            NormalizarIds();
        }

        private void NormalizarIds()
        {
            var productos = ObtenerTodos();
            var idsUsados = new HashSet<int>(productos.Where(x => x.idProducto > 0).Select(x => x.idProducto));
            var siguienteId = idsUsados.Count == 0 ? 1 : idsUsados.Max() + 1;
            var huboCambios = false;

            foreach (var producto in productos)
            {
                if (producto.idProducto > 0 && productos.Count(x => x.idProducto == producto.idProducto) == 1)
                    continue;

                while (idsUsados.Contains(siguienteId))
                    siguienteId++;

                producto.idProducto = siguienteId++;
                idsUsados.Add(producto.idProducto);
                huboCambios = true;
            }

            if (huboCambios)
                ReemplazarTodos(productos);
        }

        private static IEnumerable<Producto> CrearProductosIniciales()
        {
            return new[]
            {
                new Producto { idProducto = 1, Nombre = "Remera Running Dry", Tipo = "Remera", Talle = "M", Color = "Negro", Precio = 18500m, Stock = 25, Activo = true },
                new Producto { idProducto = 2, Nombre = "Remera Training Pro", Tipo = "Remera", Talle = "L", Color = "Azul", Precio = 21000m, Stock = 18, Activo = true },
                new Producto { idProducto = 3, Nombre = "Calza Corta Active", Tipo = "Calza", Talle = "S", Color = "Gris", Precio = 19500m, Stock = 14, Activo = true },
                new Producto { idProducto = 4, Nombre = "Calza Larga Winter", Tipo = "Calza", Talle = "M", Color = "Negro", Precio = 28000m, Stock = 12, Activo = true },
                new Producto { idProducto = 5, Nombre = "Short Deportivo Flex", Tipo = "Short", Talle = "L", Color = "Bordo", Precio = 17500m, Stock = 20, Activo = true },
                new Producto { idProducto = 6, Nombre = "Buzo Hoodie Sport", Tipo = "Buzo", Talle = "XL", Color = "Verde", Precio = 42000m, Stock = 9, Activo = true },
                new Producto { idProducto = 7, Nombre = "Campera Rompeviento", Tipo = "Campera", Talle = "M", Color = "Azul", Precio = 52000m, Stock = 7, Activo = true },
                new Producto { idProducto = 8, Nombre = "Conjunto Deportivo", Tipo = "Conjunto", Talle = "L", Color = "Negro", Precio = 46500m, Stock = 11, Activo = true },
                new Producto { idProducto = 9, Nombre = "Top Deportivo Mujer", Tipo = "Top", Talle = "S", Color = "Rosa", Precio = 16000m, Stock = 16, Activo = true },
                new Producto { idProducto = 10, Nombre = "Medias Running Pack", Tipo = "Accesorio", Talle = "Único", Color = "Blanco", Precio = 8500m, Stock = 30, Activo = true }
            };
        }
    }
}
