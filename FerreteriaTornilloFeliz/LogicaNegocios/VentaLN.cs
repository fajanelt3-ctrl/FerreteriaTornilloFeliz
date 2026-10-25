using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using FerreteriaTornilloFeliz.Entidades;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Lógica de negocio de las ventas.
    /// RegistrarVenta es una operación atómica: guarda la venta, sus detalles y descuenta el stock
    /// dentro de UNA transacción SQL. Si cualquier paso falla, no se guarda nada (rollback).
    /// </summary>
    public class VentaLN
    {
        private const int CantidadMaximaPorLinea = 1000000;

        /// <summary>
        /// Registra la venta y devuelve su Id. La lógica NO confía en los precios que trae la pantalla:
        /// vuelve a leer precio, costo y stock reales desde SQL Server dentro de la transacción.
        /// </summary>
        public int RegistrarVenta(Venta venta)
        {
            if (venta == null)
                throw new ReglaNegocioException("No se recibieron los datos de la venta.");
            if (venta.Detalles.Count == 0)
                throw new ReglaNegocioException("La venta debe tener al menos un producto.");

            // 1. Consolidar cantidades por producto (por si un mismo producto llega en varias líneas).
            Dictionary<int, int> cantidades = new Dictionary<int, int>();
            foreach (DetalleVenta d in venta.Detalles)
            {
                if (d.Cantidad <= 0)
                    throw new ReglaNegocioException("La cantidad de cada producto debe ser mayor que cero.");
                if (d.Cantidad > CantidadMaximaPorLinea)
                    throw new ReglaNegocioException("La cantidad de un producto no puede superar " + CantidadMaximaPorLinea + ".");

                if (cantidades.ContainsKey(d.IdProducto))
                    cantidades[d.IdProducto] += d.Cantidad;
                else
                    cantidades.Add(d.IdProducto, d.Cantidad);
            }

            // 2. Ordenar por Id: todas las ventas bloquean los productos en el mismo orden,
            //    lo que evita interbloqueos (deadlocks) entre ventas simultáneas.
            List<int> ids = new List<int>(cantidades.Keys);
            ids.Sort();

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();

                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        // 3. Leer y bloquear cada producto, validando existencia, estado y stock.
                        List<DetalleVenta> lineas = new List<DetalleVenta>();
                        decimal total = 0;

                        foreach (int idProducto in ids)
                        {
                            DetalleVenta linea = LeerYBloquearProducto(cn, tx, idProducto, cantidades[idProducto]);
                            lineas.Add(linea);
                            total += linea.Subtotal;
                        }

                        // 4. Guardar la venta, sus detalles y descontar el stock.
                        int idVenta = InsertarVenta(cn, tx, total);

                        foreach (DetalleVenta linea in lineas)
                        {
                            InsertarDetalle(cn, tx, idVenta, linea);
                            DescontarStock(cn, tx, linea);
                        }

                        // 5. Todo salió bien: confirmar.
                        tx.Commit();

                        venta.Id = idVenta;
                        venta.Total = total;
                        return idVenta;
                    }
                    catch
                    {
                        try { tx.Rollback(); }
                        catch { /* si la conexión ya se perdió, SQL Server revierte por su cuenta */ }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Lista las ventas no anuladas entre dos fechas (inclusive), de la más reciente a la más antigua.
        /// Las ventas devueltas traen Id, Fecha y Total; sus detalles no se cargan.
        /// </summary>
        public List<Venta> ListarPorFechas(DateTime desde, DateTime hasta)
        {
            if (desde.Date > hasta.Date)
                throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la fecha final.");

            List<Venta> lista = new List<Venta>();

            const string sql =
                "SELECT Id, Fecha, Total, Anulada FROM Ventas " +
                "WHERE Anulada = 0 AND Fecha >= @inicio AND Fecha < @fin " +
                "ORDER BY Fecha DESC, Id DESC";

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@inicio", desde.Date);
                cmd.Parameters.AddWithValue("@fin", hasta.Date.AddDays(1));
                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Venta
                        {
                            Id = dr.GetInt32(0),
                            Fecha = dr.GetDateTime(1),
                            Total = dr.GetDecimal(2),
                            Anulada = dr.GetBoolean(3)
                        });
                    }
                }
            }

            return lista;
        }

        // ---------- Pasos de la transacción ----------

        // Lee el producto con UPDLOCK: nadie más puede cambiar su stock hasta que termine esta transacción.
        private static DetalleVenta LeerYBloquearProducto(SqlConnection cn, SqlTransaction tx, int idProducto, int cantidad)
        {
            const string sql =
                "SELECT Codigo, Nombre, PrecioCompra, PrecioVenta, Stock, Activo " +
                "FROM Productos WITH (UPDLOCK, ROWLOCK) WHERE Id = @id";

            using (SqlCommand cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.AddWithValue("@id", idProducto);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                        throw new ReglaNegocioException("El producto con Id " + idProducto + " no existe.");

                    string codigo = dr.GetString(0);
                    string nombre = dr.GetString(1);
                    decimal precioCompra = dr.GetDecimal(2);
                    decimal precioVenta = dr.GetDecimal(3);
                    int stock = dr.GetInt32(4);
                    bool activo = dr.GetBoolean(5);

                    if (!activo)
                        throw new ReglaNegocioException(
                            "El producto \"" + nombre + "\" está desactivado y no se puede vender.");

                    if (cantidad > stock)
                        throw new ReglaNegocioException(
                            "Stock insuficiente de \"" + nombre + "\": disponible " + stock +
                            ", solicitado " + cantidad + ". No se registró la venta.");

                    return new DetalleVenta
                    {
                        IdProducto = idProducto,
                        CodigoProducto = codigo,
                        NombreProducto = nombre,
                        Cantidad = cantidad,
                        PrecioUnitario = precioVenta,
                        CostoUnitario = precioCompra
                    };
                }
            }
        }

        // Fecha y Anulada toman sus valores por defecto de la tabla (GETDATE() y 0).
        private static int InsertarVenta(SqlConnection cn, SqlTransaction tx, decimal total)
        {
            const string sql =
                "INSERT INTO Ventas (Total) VALUES (@total); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlCommand cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.AddWithValue("@total", total);
                return (int)cmd.ExecuteScalar();
            }
        }

        private static void InsertarDetalle(SqlConnection cn, SqlTransaction tx, int idVenta, DetalleVenta linea)
        {
            const string sql =
                "INSERT INTO DetalleVenta (IdVenta, IdProducto, Cantidad, PrecioUnitario, CostoUnitario) " +
                "VALUES (@idVenta, @idProducto, @cantidad, @precio, @costo)";

            using (SqlCommand cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idVenta", idVenta);
                cmd.Parameters.AddWithValue("@idProducto", linea.IdProducto);
                cmd.Parameters.AddWithValue("@cantidad", linea.Cantidad);
                cmd.Parameters.AddWithValue("@precio", linea.PrecioUnitario);
                cmd.Parameters.AddWithValue("@costo", linea.CostoUnitario);
                cmd.ExecuteNonQuery();
            }
        }

        // La condición "Stock >= @cantidad" es la última defensa: el stock nunca queda negativo.
        private static void DescontarStock(SqlConnection cn, SqlTransaction tx, DetalleVenta linea)
        {
            const string sql =
                "UPDATE Productos SET Stock = Stock - @cantidad " +
                "WHERE Id = @id AND Stock >= @cantidad";

            using (SqlCommand cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.AddWithValue("@cantidad", linea.Cantidad);
                cmd.Parameters.AddWithValue("@id", linea.IdProducto);

                if (cmd.ExecuteNonQuery() != 1)
                    throw new ReglaNegocioException(
                        "No se pudo descontar el stock de \"" + linea.NombreProducto +
                        "\" porque las existencias cambiaron. Intente de nuevo.");
            }
        }
    }
}
