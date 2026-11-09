using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SistemaCaminosalDeporte.Datos;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    /// <summary>Administra autenticación, usuarios, roles y contraseñas hash.</summary>
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos datos;

        public UsuarioNegocio()
            : this(new UsuarioDatos())
        {
        }

        public UsuarioNegocio(UsuarioDatos datos)
        {
            this.datos = datos ?? throw new ArgumentNullException(nameof(datos));
        }

        public Resultado<System.Collections.Generic.List<Usuario>> Listar(bool incluirInactivos = false)
        {
            try
            {
                var usuarios = datos.ObtenerTodos();
                if (!incluirInactivos)
                    usuarios = usuarios.Where(x => x.Activo).ToList();
                return Resultado<System.Collections.Generic.List<Usuario>>.Ok(usuarios);
            }
            catch (Exception ex)
            {
                return Resultado<System.Collections.Generic.List<Usuario>>.Error("No se pudieron listar los usuarios: " + ex.Message);
            }
        }

        /// <summary>Valida credenciales y abre la sesión del usuario activo.</summary>
        public Resultado<Usuario> Login(string usuario, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(contraseña))
                return Resultado<Usuario>.Error("Debe ingresar usuario y contraseña.");

            try
            {
                var hash = CalcularSha256(contraseña);
                var encontrado = datos.ObtenerTodos().FirstOrDefault(x =>
                    x.Activo &&
                    string.Equals(x.NombreUsuario, usuario.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.ContraseñaHash, hash, StringComparison.OrdinalIgnoreCase));

                if (encontrado == null)
                    return Resultado<Usuario>.Error("Usuario, contraseña o estado de cuenta incorrectos.");

                SesionActual.Iniciar(encontrado);
                return Resultado<Usuario>.Ok(encontrado, "Inicio de sesión correcto.");
            }
            catch (Exception ex)
            {
                return Resultado<Usuario>.Error("No se pudo iniciar sesión: " + ex.Message);
            }
        }

        /// <summary>Crea un usuario activo almacenando la contraseña como SHA-256.</summary>
        public Resultado<Usuario> Alta(string nombre, string nombreUsuario, string contraseña, string rol)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(nombreUsuario))
                return Resultado<Usuario>.Error("Nombre y usuario son obligatorios.");
            if (string.IsNullOrEmpty(contraseña))
                return Resultado<Usuario>.Error("La contraseña es obligatoria.");
            if (!RolValido(rol))
                return Resultado<Usuario>.Error("El rol debe ser Dueño, Empleado o Administrador.");

            try
            {
                if (datos.ObtenerTodos().Any(x => string.Equals(x.NombreUsuario, nombreUsuario.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return Resultado<Usuario>.Error("Ya existe un usuario con ese nombre de usuario.");

                var nuevo = new Usuario
                {
                    Nombre = nombre.Trim(),
                    NombreUsuario = nombreUsuario.Trim(),
                    ContraseñaHash = CalcularSha256(contraseña),
                    Rol = rol,
                    Activo = true
                };
                datos.Agregar(nuevo);
                return Resultado<Usuario>.Ok(nuevo, "Usuario creado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Usuario>.Error("No se pudo crear el usuario: " + ex.Message);
            }
        }

        public Resultado<Usuario> Editar(int idUsuario, string nombre, string nombreUsuario, string rol, string contraseña = null)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(nombreUsuario))
                return Resultado<Usuario>.Error("Nombre y usuario son obligatorios.");
            if (string.IsNullOrEmpty(contraseña))
                return Resultado<Usuario>.Error("La contraseña no puede quedar vacía.");
            if (!RolValido(rol))
                return Resultado<Usuario>.Error("El rol debe ser Dueño, Empleado o Administrador.");

            try
            {
                var usuario = datos.ObtenerPorId(idUsuario);
                if (usuario == null)
                    return Resultado<Usuario>.Error("No se encontró el usuario.");

                var duplicado = datos.ObtenerTodos().Any(x =>
                    x.idUsuario != idUsuario &&
                    string.Equals(x.NombreUsuario, nombreUsuario.Trim(), StringComparison.OrdinalIgnoreCase));
                if (duplicado)
                    return Resultado<Usuario>.Error("Ya existe otro usuario con ese nombre de usuario.");

                usuario.Nombre = nombre.Trim();
                usuario.NombreUsuario = nombreUsuario.Trim();
                usuario.Rol = rol;
                usuario.ContraseñaHash = CalcularSha256(contraseña);
                datos.Guardar();
                return Resultado<Usuario>.Ok(usuario, "Usuario actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado<Usuario>.Error("No se pudo actualizar el usuario: " + ex.Message);
            }
        }

        /// <summary>Desactiva un usuario salvo que sea el de la sesión actual.</summary>
        public Resultado Desactivar(int idUsuario)
        {
            if (SesionActual.Usuario != null && SesionActual.Usuario.idUsuario == idUsuario)
                return Resultado.Error("No se puede desactivar el usuario que tiene la sesión abierta.");

            try
            {
                var usuario = datos.ObtenerPorId(idUsuario);
                if (usuario == null)
                    return Resultado.Error("No se encontró el usuario.");

                usuario.Activo = false;
                datos.Guardar();
                return Resultado.Ok("Usuario desactivado correctamente.");
            }
            catch (Exception ex)
            {
                return Resultado.Error("No se pudo desactivar el usuario: " + ex.Message);
            }
        }

        private static bool RolValido(string rol)
        {
            return rol == "Dueño" || rol == "Empleado" || rol == "Administrador";
        }

        internal static string CalcularSha256(string texto)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                var resultado = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                    resultado.Append(b.ToString("x2"));
                return resultado.ToString();
            }
        }
    }
}
