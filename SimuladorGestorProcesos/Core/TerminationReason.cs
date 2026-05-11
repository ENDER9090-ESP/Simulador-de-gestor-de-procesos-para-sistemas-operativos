// ============================================================================
// Archivo: TerminationReason.cs
// Descripción: Enum que clasifica las causas por las que un proceso
//              puede ser terminado en el simulador.
// ============================================================================

namespace SimuladorGestorProcesos.Core
{
    /// <summary>
    /// Razones por las que un proceso puede finalizar su ejecución.
    /// Se asigna al PCB al momento de la terminación para trazabilidad.
    /// </summary>
    public enum TerminationReason
    {
        /// <summary>
        /// El proceso completó su ejecución normalmente
        /// (RemainingTime llegó a 0).
        /// </summary>
        Normal,

        /// <summary>
        /// El proceso fue terminado forzosamente por el usuario
        /// (equivalente a un 'kill' desde la consola).
        /// </summary>
        ForcedByUser,

        /// <summary>
        /// El proceso fue terminado debido a un error de ejecución
        /// (e.g., acceso inválido a memoria, excepción no manejada).
        /// </summary>
        Error,

        /// <summary>
        /// El proceso fue terminado porque el sistema detectó una
        /// situación de interbloqueo (deadlock) irrecuperable.
        /// </summary>
        Deadlock
    }
}
