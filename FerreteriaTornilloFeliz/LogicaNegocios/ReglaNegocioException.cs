using System;

namespace FerreteriaTornilloFeliz.LogicaNegocios
{
    /// <summary>
    /// Se lanza cuando una operación incumple una regla de negocio o una validación
    /// (por ejemplo: código duplicado o precio negativo). Su mensaje se muestra al usuario.
    /// </summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje)
        {
        }
    }
}
