namespace FerreteriaTornilloFeliz.Entidades
{
    /// <summary>
    /// Resumen de lo vendido de un producto en un período (para el reporte "Por producto").
    /// </summary>
    public class ProductoVendido
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Unidades { get; set; }
        public decimal Ingreso { get; set; }
        public decimal Costo { get; set; }

        /// <summary>Ganancia del producto: ingreso menos costo.</summary>
        public decimal Utilidad
        {
            get { return Ingreso - Costo; }
        }
    }
}
