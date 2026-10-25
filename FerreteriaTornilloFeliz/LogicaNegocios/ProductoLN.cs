using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using FerreteriaTornilloFeliz.Entidades;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Lógica de negocio de los productos: valida las reglas y guarda/consulta en SQL Server.
    /// Todas las consultas usan parámetros (protección contra inyección SQL).
    /// </summary>
    public class ProductoLN
    {
        private const string Columnas =
            "Id, Codigo, Nombre, Categoria, PrecioCompra, PrecioVenta, Stock, StockMinimo, Activo";

        // ---------- Validaciones (reglas de negocio) ----------

        private static void Validar(Producto p)
        {
            if (p == null)
                throw new ReglaNegocioException("No se recibieron los datos del producto.");

            if (string.IsNullOrWhiteSpace(p.Codigo))
                throw new ReglaNegocioException("El código es obligatorio.");
            if (p.Codigo.Trim().Length > 20)
                throw new ReglaNegocioException("El código no puede tener más de 20 caracteres.");

            if (string.IsNullOrWhiteSpace(p.Nombre))
                throw new ReglaNegocioException("El nombre es obligatorio.");
            if (p.Nombre.Trim().Length > 100)
                throw new ReglaNegocioException("El nombre no puede tener más de 100 caracteres.");

            if (string.IsNullOrWhiteSpace(p.Categoria))
                throw new ReglaNegocioException("La categoría es obligatoria.");
            if (p.Categoria.Trim().Length > 50)
                throw new ReglaNegocioException("La categoría no puede tener más de 50 caracteres.");

            if (p.PrecioCompra < 0)
                throw new ReglaNegocioException("El precio de compra no puede ser negativo.");
            if (p.PrecioVenta <= 0)
                throw new ReglaNegocioException("El precio de venta debe ser mayor que cero.");
            if (p.PrecioCompra > 1000000 || p.PrecioVenta > 1000000)
                throw new ReglaNegocioException("Los precios no pueden superar Q1,000,000.");
            if (p.PrecioVenta < p.PrecioCompra)
                throw new ReglaNegocioException("El precio de venta no puede ser menor que el precio de compra.");

            if (p.Stock < 0)
                throw new ReglaNegocioException("El stock no puede ser negativo.");
            if (p.StockMinimo < 0)
                throw new ReglaNegocioException("El stock mínimo no puede ser negativo.");
        }

        // ---------- Operaciones de escritura ----------

        /// <summary>Registra un producto nuevo. Rechaza códigos repetidos.</summary>
        public void Registrar(Producto p)
        {
            Validar(p);
            string codigo = p.Codigo.Trim();

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();

                if (ExisteCodigo(cn, codigo, 0))
                    throw new ReglaNegocioException(
                        "Ya existe un producto con el código " + codigo + " (puede estar desactivado).");

                const string sql =
                    "INSERT INTO Productos (Codigo, Nombre, Categoria, PrecioCompra, PrecioVenta, Stock, StockMinimo, Activo) " +
                    "VALUES (@codigo, @nombre, @categoria, @compra, @venta, @stock, @minimo, 1)";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    AsignarParametros(cmd, p);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Actualiza un producto existente. Rechaza códigos que ya usa otro producto.</summary>
        public void Actualizar(Producto p)
        {
            Validar(p);
            string codigo = p.Codigo.Trim();

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();

                if (ExisteCodigo(cn, codigo, p.Id))
                    throw new ReglaNegocioException(
                        "Ya existe otro producto con el código " + codigo + " (puede estar desactivado).");

                const string sql =
                    "UPDATE Productos SET Codigo = @codigo, Nombre = @nombre, Categoria = @categoria, " +
                    "PrecioCompra = @compra, PrecioVenta = @venta, Stock = @stock, StockMinimo = @minimo " +
                    "WHERE Id = @id AND Activo = 1";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    AsignarParametros(cmd, p);
                    cmd.Parameters.AddWithValue("@id", p.Id);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new ReglaNegocioException("El producto ya no existe o fue desactivado.");
                }
            }
        }

        /// <summary>
        /// "Eliminar" un producto lo desactiva (Activo = 0). Así se conserva el historial de ventas
        /// y los reportes no pierden información.
        /// </summary>
        public void Desactivar(int id)
        {
            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            {
                cn.Open();
                const string sql = "UPDATE Productos SET Activo = 0 WHERE Id = @id AND Activo = 1";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new ReglaNegocioException("El producto ya no existe o ya estaba desactivado.");
                }
            }
        }

        // ---------- Consultas ----------

        /// <summary>Lista los productos activos, ordenados por nombre.</summary>
        public List<Producto> Listar()
        {
            return Consultar(
                "SELECT " + Columnas + " FROM Productos WHERE Activo = 1 ORDER BY Nombre", null);
        }

        /// <summary>Busca productos activos cuyo código o nombre contenga el texto.</summary>
        public List<Producto> Buscar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Listar();

            return Consultar(
                "SELECT " + Columnas + " FROM Productos " +
                "WHERE Activo = 1 AND (Codigo LIKE @texto OR Nombre LIKE @texto) ORDER BY Nombre",
                "%" + texto.Trim() + "%");
        }

        /// <summary>Devuelve el producto activo con ese código exacto, o null si no existe.</summary>
        public Producto ObtenerPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return null;

            List<Producto> lista = Consultar(
                "SELECT " + Columnas + " FROM Productos WHERE Activo = 1 AND Codigo = @texto",
                codigo.Trim());

            return lista.Count > 0 ? lista[0] : null;
        }

        /// <summary>Lista los productos activos con stock en el mínimo o por debajo (alertas).</summary>
        public List<Producto> ListarBajoMinimo()
        {
            return Consultar(
                "SELECT " + Columnas + " FROM Productos " +
                "WHERE Activo = 1 AND Stock <= StockMinimo ORDER BY Stock, Nombre", null);
        }

        // ---------- Métodos auxiliares privados ----------

        private static bool ExisteCodigo(SqlConnection cn, string codigo, int idExcluido)
        {
            const string sql = "SELECT COUNT(*) FROM Productos WHERE Codigo = @codigo AND Id <> @id";

            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@codigo", codigo);
                cmd.Parameters.AddWithValue("@id", idExcluido);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private static void AsignarParametros(SqlCommand cmd, Producto p)
        {
            cmd.Parameters.AddWithValue("@codigo", p.Codigo.Trim());
            cmd.Parameters.AddWithValue("@nombre", p.Nombre.Trim());
            cmd.Parameters.AddWithValue("@categoria", p.Categoria.Trim());
            cmd.Parameters.AddWithValue("@compra", Math.Round(p.PrecioCompra, 2));
            cmd.Parameters.AddWithValue("@venta", Math.Round(p.PrecioVenta, 2));
            cmd.Parameters.AddWithValue("@stock", p.Stock);
            cmd.Parameters.AddWithValue("@minimo", p.StockMinimo);
        }

        private static List<Producto> Consultar(string sql, string parametroTexto)
        {
            List<Producto> lista = new List<Producto>();

            using (SqlConnection cn = ConexionBD.ObtenerConexion())
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                if (parametroTexto != null)
                    cmd.Parameters.AddWithValue("@texto", parametroTexto);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Mapear(dr));
                }
            }

            return lista;
        }

        // El orden de las columnas coincide con la constante Columnas.
        private static Producto Mapear(SqlDataReader dr)
        {
            return new Producto(
                dr.GetInt32(0),     // Id
                dr.GetString(1),    // Codigo
                dr.GetString(2),    // Nombre
                dr.GetString(3),    // Categoria
                dr.GetDecimal(4),   // PrecioCompra
                dr.GetDecimal(5),   // PrecioVenta
                dr.GetInt32(6),     // Stock
                dr.GetInt32(7),     // StockMinimo
                dr.GetBoolean(8));  // Activo
        }
    }
}
