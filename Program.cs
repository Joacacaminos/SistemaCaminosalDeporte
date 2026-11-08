using System;
using System.Windows.Forms;
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

            using (var login = new FrmLogin())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
                Application.Run(new FrmPrincipal(login.UsuarioIngresado, login.RolSeleccionado));
            }
        }
    }
}