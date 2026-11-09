using System;
using System.Collections.Generic;
using System.Linq;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    /// <summary>Reglas de alta, consulta, modificación y baja lógica de productos.</summary>
    public class ProductoNegocio
    {
        private readonly ProductoDatos datos;

        public ProductoNegocio()
            : this(new ProductoDatos())
        {
        }

        public ProductoNegocio(ProductoDatos datos)
        {
            this.datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        /// <summary>Lista productos activos salvo que se soliciten también los inactivos.</summary>
        public Resultado<List<Producto>> Listar(bool incluirInactivos = false)
        {
            try
            {
                var productos = datos.ObtenerTodos();
                if (!incluirInactivos)
                    productos = productos.Where(x => x.Activo).ToList();
                return Resultado<List<Producto>>.Ok(productos);
            }
            catch (Exception ex)
            {
                return Resultado<List<Producto>>.Error("No se pudieron listar los productos: " + ex.Message);
            }
        }

        public Resultado<List<Producto>> Buscar(string texto, bool incluirInactivos = false)
        {
            try
            {
                var criterio = (texto ?? string.Empty).Trim();
                var productos = datos.ObtenerTodos()
                    .Where(x => (incluirInactivos || x.Activo) &&
                        (criterio.Length == 0 ||
                         Contiene(x.Nombre, criterio) ||
                         Contiene(x.Talle, criterio) ||
                         Contiene(x.Tipo, criterio)))
                    .ToList();
                return Resultado<List<Producto>>.Ok(productos);
            }
            catch (Exception ex)
            {
                return Resultado<List<Producto>>.Error("No se pudieron buscar los productos: " + ex.Message);
            }
        }

        /// <summary>Valida y registra un producto nuevo.</summary>
        public Resultado<Producto> Alta(Producto producto)
        {
            var validacion = Validar(producto);
            if (!validacion.Exito)
                return Resultado<Producto>.Error(validacion.Mensaje);

            try
            {
                producto.Activo = true;
                datos.Agregar(producto);
                return Resultado<Producto>.Ok(producto, "Producto creado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Producto>.Error("No se pudo crear el producto: " + ex.Message);
            }
        }

        /// <summary>Actualiza los datos de un producto existente.</summary>
        public Resultado<Producto> Modificar(Producto producto)
        {
            var validacion = Validar(producto);
            if (!validacion.Exito)
                return Resultado<Producto>.Error(validacion.Mensaje);

            try
            {
                var existente = datos.ObtenerPorId(producto.idProducto);
                if (existente == null)
                    return Resultado<Producto>.Error("No se encontró el producto.");

                existente.Nombre = producto.Nombre.Trim();
                existente.Tipo = producto.Tipo.Trim();
                existente.Talle = producto.Talle.Trim();
                existente.Color = producto.Color.Trim();
                existente.Precio = producto.Precio;
                existente.Stock = producto.Stock;
                datos.Guardar();
                return Resultado<Producto>.Ok(existente, "Producto actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Producto>.Error("No se pudo actualizar el producto: " + ex.Message);
            }
        }

        /// <summary>Desactiva un producto sin eliminarlo del XML.</summary>
        public Resultado BajaLogica(int idProducto)
        {
            try
            {
                var producto = datos.ObtenerPorId(idProducto);
                if (producto == null)
                    return Resultado.Error("No se encontró el producto.");

                producto.Activo = false;
                datos.Guardar();
                return Resultado.Ok("Producto dado de baja correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudo dar de baja el producto: " + ex.Message);
            }
        }

        internal ProductoDatos ObtenerDatos()
        {
            return datos;
        }

        private static Resultado Validar(Producto producto)
        {
            if (producto == null)
                return Resultado.Error("El producto es obligatorio.");
            if (string.IsNullOrWhiteSpace(producto.Nombre) ||
                string.IsNullOrWhiteSpace(producto.Tipo) ||
                string.IsNullOrWhiteSpace(producto.Talle) ||
                string.IsNullOrWhiteSpace(producto.Color))
                return Resultado.Error("Nombre, tipo, talle y color son obligatorios.");
            if (producto.Precio <= 0)
                return Resultado.Error("El precio debe ser mayor a cero.");
            if (producto.Stock < 0)
                return Resultado.Error("El stock no puede ser negativo.");
            return Resultado.Ok();
        }

        private static bool Contiene(string valor, string criterio)
        {
            return !string.IsNullOrEmpty(valor) &&
                valor.IndexOf(criterio, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
