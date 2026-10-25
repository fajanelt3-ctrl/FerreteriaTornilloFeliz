using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FerreteriaTornilloFeliz.Presentacion
{
    /// <summary>
    /// Carga el logo del sistema desde la carpeta Recursos junto al ejecutable.
    /// Si el archivo no existe, devuelve null y el sistema funciona igual, sin logo.
    /// </summary>
    public static class Logo
    {
        private static string Ruta(string archivo)
        {
            return Path.Combine(Application.StartupPath, "Recursos", archivo);
        }

        public static Image ObtenerImagen()
        {
            try
            {
                string ruta = Ruta("logo.png");
                return File.Exists(ruta) ? Image.FromFile(ruta) : null;
            }
            catch
            {
                return null;
            }
        }

        public static Icon ObtenerIcono()
        {
            try
            {
                string ruta = Ruta("logo.ico");
                return File.Exists(ruta) ? new Icon(ruta) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
