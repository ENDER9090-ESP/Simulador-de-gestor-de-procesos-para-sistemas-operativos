// ============================================================================
// Archivo: ProcessState.cs
// Descripción: Define los estados válidos del ciclo de vida de un proceso
//              dentro del simulador del gestor de procesos.
// ============================================================================

namespace SimuladorGestorProcesos.Core
{
    /// <summary>
    /// Enumeración que representa los estados fundamentales de un proceso
    /// según el modelo clásico de 5 estados de los sistemas operativos.
    /// </summary>
    public enum ProcessState
    {
        /// <summary>
        /// El proceso ha sido creado pero aún no ha sido admitido
        /// en la cola de procesos listos por el planificador a largo plazo.
        /// </summary>
        Nuevo,

        /// <summary>
        /// El proceso está en memoria principal y listo para ser
        /// asignado al procesador (CPU) por el planificador a corto plazo.
        /// </summary>
        Listo,

        /// <summary>
        /// El proceso está actualmente utilizando la CPU.
        /// Solo un proceso puede estar en este estado por cada núcleo de CPU.
        /// </summary>
        Ejecutando,

        /// <summary>
        /// El proceso está bloqueado esperando la finalización de un evento
        /// de E/S o la disponibilidad de un recurso.
        /// </summary>
        Esperando,

        /// <summary>
        /// El proceso ha completado su ejecución o ha sido terminado
        /// explícitamente. Sus recursos pueden ser liberados.
        /// </summary>
        Terminado
    }
}
