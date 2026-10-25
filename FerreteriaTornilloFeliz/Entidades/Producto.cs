namespace FerreteriaTornilloFeliz.Entidades
{
    /// <summary>
    /// Representa un producto del inventario de la ferretería.
    /// Es una entidad: solo guarda datos, no accede a la base de datos.
    /// </summary>
    public class Producto
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
        public bool Activo { get; set; }

        /// <summary>Constructor vacío: un producto nuevo nace activo.</summary>
        public Producto()
        {
            Activo = true;
        }

        /// <summary>Constructor completo, usado al leer un producto desde la base de datos.</summary>
        public Producto(int id, string codigo, string nombre, string categoria,
                        decimal precioCompra, decimal precioVenta,
                        int stock, int stockMinimo, bool activo)
        {
            Id = id;
            Codigo = codigo;
            Nombre = nombre;
            Categoria = categoria;
            PrecioCompra = precioCompra;
            PrecioVenta = precioVenta;
            Stock = stock;
            StockMinimo = stockMinimo;
            Activo = activo;
        }

        /// <summary>Verdadero cuando el stock está en el mínimo o por debajo (alerta de reabastecimiento).</summary>
        public bool BajoMinimo
        {
            get { return Stock <= StockMinimo; }
        }
    }
}
