namespace SistemaCaminosalDeporte.Entidades
{
    public class Usuario
    {
        public int idUsuario { get; set; }
        public string Nombre { get; set; }
        public string NombreUsuario { get; set; }
        public string ContraseñaHash { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
    }
}
