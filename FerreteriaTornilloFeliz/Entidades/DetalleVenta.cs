namespace FerreteriaTornilloFeliz.Entidades
{
    /// <summary>
    /// Una línea de una venta: un producto, su cantidad y los precios vigentes al momento de vender.
    /// Guarda también el costo unitario, para que los reportes de utilidad no cambien si
    /// después se modifica el precio de compra del producto.
    /// </summary>
    public class DetalleVenta
    {
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; }
        public string NombreProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal CostoUnitario { get; set; }

        /// <summary>Importe de la línea: cantidad por precio unitario.</summary>
        public decimal Subtotal
        {
            get { return Cantidad * PrecioUnitario; }
        }
    }
}
