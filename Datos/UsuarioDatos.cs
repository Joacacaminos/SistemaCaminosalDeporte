using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Datos
{
    public class UsuarioDatos : RepositorioXml<Usuario>
    {
        public UsuarioDatos()
            : base(
                "Usuarios.xml",
                usuario => usuario.idUsuario,
                (usuario, id) => usuario.idUsuario = id,
                CrearUsuariosIniciales())
        {
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
                new Usuario
                {
                    idUsuario = 3,
                    Nombre = "Administrador del sistema",
                    NombreUsuario = "administrador",
                    ContraseñaHash = contraseñaHash,
                    Rol = "Administrador",
                    Activo = true
                }
            };
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
