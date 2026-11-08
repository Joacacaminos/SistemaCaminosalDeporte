using SistemaCaminosalDeporte.Entidades;

namespace SistemaCaminosalDeporte.Negocio
{
    public static class SesionActual
    {
        public static Usuario Usuario { get; private set; }
        public static string Rol { get; private set; }
        public static bool EstaIniciada { get { return Usuario != null; } }

        public static void Iniciar(Usuario usuario)
        {
            Usuario = usuario;
            Rol = usuario == null ? null : usuario.Rol;
        }

        public static void Cerrar()
        {
            Usuario = null;
            Rol = null;
        }
    }
}
