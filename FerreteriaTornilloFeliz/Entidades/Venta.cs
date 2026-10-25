using System;
using System.Collections.Generic;

namespace FerreteriaTornilloFeliz.Entidades
{
    /// <summary>
    /// Representa una venta con sus líneas de detalle (composición: una venta contiene
    /// uno o más DetalleVenta). Es una entidad: no accede a la base de datos.
    /// </summary>
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public bool Anulada { get; set; }
        public decimal Total { get; set; }

        /// <summary>Líneas de la venta. Solo se modifican con AgregarDetalle y QuitarDetalle.</summary>
        public List<DetalleVenta> Detalles { get; private set; }

        public Venta()
        {
            Detalles = new List<DetalleVenta>();
            Fecha = DateTime.Now;
        }

        /// <summary>
        /// Agrega una línea. Si el producto ya está en la venta, suma la cantidad
        /// en lugar de crear una segunda línea.
        /// </summary>
        public void AgregarDetalle(DetalleVenta nuevo)
        {
            if (nuevo == null)
                throw new ArgumentNullException("nuevo");

            foreach (DetalleVenta existente in Detalles)
            {
                if (existente.IdProducto == nuevo.IdProducto)
                {
                    existente.Cantidad += nuevo.Cantidad;
                    return;
                }
            }

            Detalles.Add(nuevo);
        }

        /// <summary>Quita de la venta la línea de ese producto.</summary>
        public void QuitarDetalle(int idProducto)
        {
            Detalles.RemoveAll(d => d.IdProducto == idProducto);
        }

        /// <summary>Suma los subtotales de todas las líneas.</summary>
        public decimal CalcularTotal()
        {
            decimal total = 0;
            foreach (DetalleVenta d in Detalles)
                total += d.Subtotal;
            return total;
        }
    }
}
