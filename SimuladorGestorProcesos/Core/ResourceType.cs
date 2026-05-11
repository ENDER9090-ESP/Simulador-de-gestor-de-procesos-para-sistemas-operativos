// ============================================================================
// Archivo: ResourceType.cs
// Descripción: Enumeración que clasifica los tipos de recursos disponibles
//              en el sistema operativo simulado. Permite identificar y
//              categorizar los recursos que se asignan a los procesos.
// ============================================================================

namespace SimuladorGestorProcesos.Core
{
    /// <summary>
    /// Tipos de recursos gestionados por el sistema operativo simulado.
    /// Cada tipo representa una categoría de hardware o recurso lógico
    /// que los procesos pueden solicitar y utilizar.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Memoria de Acceso Aleatorio. Recurso cuantificable en MB.
        /// Se asigna al crear un proceso y se libera al terminarlo.
        /// </summary>
        RAM,

        /// <summary>
        /// Unidad Central de Procesamiento. Se gestiona lógicamente
        /// a través del planificador de procesos (Scheduler).
        /// En esta versión del simulador se asume 1 núcleo disponible.
        /// </summary>
        CPU,

        /// <summary>
        /// Dispositivos de Entrada/Salida (disco, impresora, red, etc.).
        /// Reservado para futuras implementaciones del módulo de E/S.
        /// </summary>
        IO,

        /// <summary>
        /// Espacio en disco para almacenamiento persistente.
        /// Reservado para futuras implementaciones.
        /// </summary>
        Disco
    }
}
