namespace FerreteriaTornilloFeliz.Entidades
{
    /// <summary>
    /// Resultados del reporte de rentabilidad de un período. Es una entidad de transporte:
    /// los cálculos los hace ReporteLN y la pantalla solo los muestra.
    /// </summary>
    public class ResultadoRentabilidad
    {
        public int NumeroVentas { get; set; }
        public int UnidadesVendidas { get; set; }

        public decimal Ingreso { get; set; }
        public decimal Costo { get; set; }
        public decimal Utilidad { get; set; }
        public decimal MargenPorcentaje { get; set; }

        public decimal CostosFijos { get; set; }

        /// <summary>Unidades a vender para cubrir los costos fijos. Null si no se puede calcular.</summary>
        public decimal? PuntoEquilibrioUnidades { get; set; }

        /// <summary>Ventas en quetzales para cubrir los costos fijos. Null si no se puede calcular.</summary>
        public decimal? PuntoEquilibrioQuetzales { get; set; }
    }
}
