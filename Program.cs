using System;
using System.Windows.Forms;
using SistemaCaminosalDeporte.Negocio;
using SistemaCaminosalDeporte.Presentacion;

namespace SistemaCaminosalDeporte
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            while (true)
            {
                using (var login = new FrmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                }

                var usuario = SesionActual.Usuario;
                if (usuario == null)
                    return;

                using (var principal = new FrmPrincipal(usuario.Nombre, usuario.Rol))
                {
                    principal.ShowDialog();
                    if (!principal.SolicitoCerrarSesion)
                        return;
                }
            }
        }
    }
}