using System;
using System.Data.SqlClient;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Lee y guarda los parámetros del sistema. Por ahora, los costos fijos mensuales
    /// (alquiler, salarios, servicios), usados para calcular el punto de equilibrio.
    /// La tabla Configuracion tiene una sola fila (Id = 1).
    /// </summary>
    public class ConfiguracionLN
    {
        private const decimal CostosFijosMaximos = 100000000m;

        /// <summary>Devuelve los costos fijos mensuales guardados (0 si aún no hay registro).</summary>
        public decimal ObtenerCostosFijos()
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CostosFijosMensuales FROM Configuracion WHERE Id = 1", cn))
            {
                cn.Open();
                object resultado = cmd.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                    return 0m;

                return (decimal)resultado;
            }
        }

        /// <summary>Guarda los costos fijos mensuales. No acepta valores negativos.</summary>
        public void GuardarCostosFijos(decimal valor)
        {
            if (valor < 0)
                throw new ReglaNegocioException("Los costos fijos no pueden ser negativos.");
            if (valor > CostosFijosMaximos)
                throw new ReglaNegocioException("Los costos fijos no pueden superar Q100,000,000.");

            decimal redondeado = Math.Round(valor, 2);

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();

                using (SqlCommand actualizar = new SqlCommand(
                    "UPDATE Configuracion SET CostosFijosMensuales = @valor WHERE Id = 1", cn))
                {
                    actualizar.Parameters.AddWithValue("@valor", redondeado);

                    // Si por alguna razón la fila no existe, se crea.
                    if (actualizar.ExecuteNonQuery() == 0)
                    {
                        using (SqlCommand insertar = new SqlCommand(
                            "INSERT INTO Configuracion (Id, CostosFijosMensuales) VALUES (1, @valor)", cn))
                        {
                            insertar.Parameters.AddWithValue("@valor", redondeado);
                            insertar.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
