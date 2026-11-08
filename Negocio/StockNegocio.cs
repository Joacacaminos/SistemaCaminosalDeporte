using System;
using System.Collections.Generic;
using System.Linq;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    public class StockNegocio
    {
        private readonly ProductoDatos productos;
        private readonly MovimientoStockDatos movimientos;
        private readonly UsuarioDatos usuarios;

        public StockNegocio()
            : this(new ProductoDatos(), new MovimientoStockDatos(), new UsuarioDatos())
        {
        }

        public StockNegocio(ProductoDatos productos, MovimientoStockDatos movimientos, UsuarioDatos usuarios)
        {
            this.productos = productos ?? throw new ArgumentNullException(nameof(productos));
            this.movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
            this.usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        }

        public Resultado<Producto> IngresarMercaderia(int idProducto, int cantidad, int idUsuario)
        {
            if (cantidad <= 0)
                return Resultado<Producto>.Error("La cantidad de ingreso debe ser mayor a cero.");

            var usuario = ValidarUsuario(idUsuario);
            if (!usuario.Exito)
                return Resultado<Producto>.Error(usuario.Mensaje);

            try
            {
                var producto = productos.ObtenerPorId(idProducto);
                if (producto == null || !producto.Activo)
                    return Resultado<Producto>.Error("No se encontró un producto activo con ese ID.");

                var stockAnterior = producto.Stock;
                producto.Stock += cantidad;
                try
                {
                    productos.Guardar();
                    movimientos.Agregar(new MovimientoStock
                    {
                        idProducto = idProducto,
                        Tipo = "Entrada",
                        Cantidad = cantidad,
                        Fecha = DateTime.Now,
                        idUsuario = idUsuario
                    });
                }
                catch
                {
                    producto.Stock = stockAnterior;
                    productos.Guardar();
                    throw;
                }

                return Resultado<Producto>.Ok(producto, "Ingreso de mercadería registrado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Producto>.Error("No se pudo registrar el ingreso: " + ex.Message);
            }
        }

        public Resultado<Producto> AjustarCantidad(int idProducto, int nuevaCantidad, int idUsuario)
        {
            if (nuevaCantidad < 0)
                return Resultado<Producto>.Error("El stock no puede ser negativo.");

            var usuario = ValidarUsuario(idUsuario);
            if (!usuario.Exito)
                return Resultado<Producto>.Error(usuario.Mensaje);

            try
            {
                var producto = productos.ObtenerPorId(idProducto);
                if (producto == null || !producto.Activo)
                    return Resultado<Producto>.Error("No se encontró un producto activo con ese ID.");

                var diferencia = nuevaCantidad - producto.Stock;
                if (diferencia == 0)
                    return Resultado<Producto>.Ok(producto, "El stock ya tenía la cantidad indicada.");

                var stockAnterior = producto.Stock;
                producto.Stock = nuevaCantidad;
                try
                {
                    productos.Guardar();
                    movimientos.Agregar(new MovimientoStock
                    {
                        idProducto = idProducto,
                        Tipo = diferencia > 0 ? "Entrada" : "Salida",
                        Cantidad = Math.Abs(diferencia),
                        Fecha = DateTime.Now,
                        idUsuario = idUsuario
                    });
                }
                catch
                {
                    producto.Stock = stockAnterior;
                    productos.Guardar();
                    throw;
                }

                return Resultado<Producto>.Ok(producto, "Stock ajustado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Producto>.Error("No se pudo ajustar el stock: " + ex.Message);
            }
        }

        public Resultado<List<Producto>> ListarStockBajo(int limite)
        {
            if (limite < 0)
                return Resultado<List<Producto>>.Error("El límite de stock no puede ser negativo.");

            try
            {
                var resultado = productos.ObtenerTodos()
                    .Where(x => x.Activo && x.Stock <= limite)
                    .OrderBy(x => x.Stock)
                    .ToList();
                return Resultado<List<Producto>>.Ok(resultado);
            }
            catch (Exception ex)
            {
                return Resultado<List<Producto>>.Error("No se pudo consultar el stock bajo: " + ex.Message);
            }
        }

        private Resultado<Usuario> ValidarUsuario(int idUsuario)
        {
            var usuario = usuarios.ObtenerPorId(idUsuario);
            return usuario == null || !usuario.Activo
                ? Resultado<Usuario>.Error("El usuario que realiza la operación no existe o está inactivo.")
                : Resultado<Usuario>.Ok(usuario);
        }
    }
}
