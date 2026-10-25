using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using FerreteriaTornilloFeliz.Entidades;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Reportes del negocio. Las sumas las hace SQL Server (SUM y GROUP BY sobre los datos guardados);
    /// aquí se aplican las fórmulas de rentabilidad y de punto de equilibrio.
    /// Solo cuentan las ventas no anuladas, y las fechas se tratan de forma inclusiva (todo el día "hasta").
    /// </summary>
    public class ReporteLN
    {
        private readonly ConfiguracionLN _configuracionLN = new ConfiguracionLN();

        /// <summary>
        /// Calcula ingreso, costo, utilidad, margen y punto de equilibrio del período.
        ///   Ingreso  = suma de (precio unitario x cantidad)
        ///   Costo    = suma de (costo unitario x cantidad), con el costo guardado al momento de vender
        ///   Utilidad = Ingreso - Costo          Margen % = Utilidad / Ingreso x 100
        ///   Punto de equilibrio (unidades)   = costos fijos / (utilidad / unidades vendidas)
        ///   Punto de equilibrio (quetzales)  = costos fijos / (utilidad / ingreso)
        /// </summary>
        public ResultadoRentabilidad Rentabilidad(DateTime desde, DateTime hasta)
        {
            DateTime inicio, fin;
            ObtenerLimites(desde, hasta, out inicio, out fin);

            ResultadoRentabilidad r = new ResultadoRentabilidad();

            const string sql =
                "SELECT ISNULL(SUM(d.Cantidad * d.PrecioUnitario), 0), " +
                "       ISNULL(SUM(d.Cantidad * d.CostoUnitario), 0), " +
                "       ISNULL(SUM(d.Cantidad), 0), " +
                "       COUNT(DISTINCT v.Id) " +
                "FROM Ventas v INNER JOIN DetalleVenta d ON d.IdVenta = v.Id " +
                "WHERE v.Anulada = 0 AND v.Fecha >= @inicio AND v.Fecha < @fin";

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@inicio", inicio);
                cmd.Parameters.AddWithValue("@fin", fin);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        r.Ingreso = dr.GetDecimal(0);
                        r.Costo = dr.GetDecimal(1);
                        r.UnidadesVendidas = dr.GetInt32(2);
                        r.NumeroVentas = dr.GetInt32(3);
                    }
                }
            }

            r.Utilidad = r.Ingreso - r.Costo;
            r.MargenPorcentaje = r.Ingreso > 0 ? Math.Round(r.Utilidad / r.Ingreso * 100, 2) : 0m;
            r.CostosFijos = _configuracionLN.ObtenerCostosFijos();

            // El punto de equilibrio solo tiene sentido si hubo ventas con utilidad positiva.
            // En caso contrario queda en null y la pantalla muestra "No calculable" (sin dividir entre cero).
            if (r.UnidadesVendidas > 0 && r.Utilidad > 0)
            {
                decimal utilidadPorUnidad = r.Utilidad / r.UnidadesVendidas;
                r.PuntoEquilibrioUnidades = Math.Ceiling(r.CostosFijos / utilidadPorUnidad);
                r.PuntoEquilibrioQuetzales = Math.Round(r.CostosFijos / (r.Utilidad / r.Ingreso), 2);
            }

            return r;
        }

        /// <summary>Productos vendidos en el período, de más a menos unidades.</summary>
        public List<ProductoVendido> MasVendidos(DateTime desde, DateTime hasta)
        {
            DateTime inicio, fin;
            ObtenerLimites(desde, hasta, out inicio, out fin);

            List<ProductoVendido> lista = new List<ProductoVendido>();

            const string sql =
                "SELECT p.Codigo, p.Nombre, SUM(d.Cantidad), " +
                "       SUM(d.Cantidad * d.PrecioUnitario), SUM(d.Cantidad * d.CostoUnitario) " +
                "FROM DetalleVenta d " +
                "INNER JOIN Ventas v ON v.Id = d.IdVenta " +
                "INNER JOIN Productos p ON p.Id = d.IdProducto " +
                "WHERE v.Anulada = 0 AND v.Fecha >= @inicio AND v.Fecha < @fin " +
                "GROUP BY p.Codigo, p.Nombre " +
                "ORDER BY SUM(d.Cantidad) DESC, p.Nombre";

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@inicio", inicio);
                cmd.Parameters.AddWithValue("@fin", fin);
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ProductoVendido
                        {
                            Codigo = dr.GetString(0),
                            Nombre = dr.GetString(1),
                            Unidades = dr.GetInt32(2),
                            Ingreso = dr.GetDecimal(3),
                            Costo = dr.GetDecimal(4)
                        });
                    }
                }
            }

            return lista;
        }

        // El rango es inclusivo: desde el inicio del día "desde" hasta el final del día "hasta".
        private static void ObtenerLimites(DateTime desde, DateTime hasta, out DateTime inicio, out DateTime fin)
        {
            if (desde.Date > hasta.Date)
                throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la fecha final.");

            inicio = desde.Date;
            fin = hasta.Date.AddDays(1);
        }
    }
}
