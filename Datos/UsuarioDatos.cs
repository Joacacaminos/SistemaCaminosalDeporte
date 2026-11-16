using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    /// <summary>Repositorio XML de usuarios y credenciales iniciales.</summary>
    public class UsuarioDatos : RepositorioXml<Usuario>
    {
        public UsuarioDatos()
            : base(
                "Usuarios.xml",
                usuario => usuario.idUsuario,
                (usuario, id) => usuario.idUsuario = id,
                CrearUsuariosIniciales())
        {
            MigrarRolesAdministrador();
        }

        private static IEnumerable<Usuario> CrearUsuariosIniciales()
        {
            var contraseñaHash = CalcularSha256("1234");
            return new[]
            {
                new Usuario
                {
                    idUsuario = 1,
                    Nombre = "Dueño de Caminos",
                    NombreUsuario = "dueno",
                    ContraseñaHash = contraseñaHash,
                    Rol = "Dueño",
                    Activo = true
                },
                new Usuario
                {
                    idUsuario = 2,
                    Nombre = "Empleado de ventas",
                    NombreUsuario = "empleado",
                    ContraseñaHash = contraseñaHash,
                    Rol = "Empleado",
                    Activo = true
                },
            };
        }

        private void MigrarRolesAdministrador()
        {
            var usuarios = ObtenerTodos();
            var modificados = false;
            foreach (var usuario in usuarios)
            {
                if (string.Equals(usuario.Rol, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    usuario.Rol = "Dueño";
                    modificados = true;
                }
            }
            if (modificados)
                ReemplazarTodos(usuarios);
        }

        private static string CalcularSha256(string texto)
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
