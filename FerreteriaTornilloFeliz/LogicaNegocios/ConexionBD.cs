using System.Configuration;
using System.Data.SqlClient;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Crea conexiones a SQL Server usando la cadena "conexionFerreteria" de App.config.
    /// Cada clase de lógica abre y cierra su propia conexión con "using".
    /// </summary>
    public static class ConexionBD
    {
        public static SqlConnection ObtenerConexion()
        {
            string cadena = ConfigurationManager.ConnectionStrings["conexionFerreteria"].ConnectionString;
            return new SqlConnection(cadena);
        }
    }
}
