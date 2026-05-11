// ============================================================================
// Archivo: IScheduler.cs
// Descripción: Interfaz que define el contrato para cualquier algoritmo
//              de planificación de CPU (Patrón de Diseño Strategy).
//              Permite intercambiar algoritmos (FCFS, Round Robin, etc.)
//              sin modificar el código del ProcessManager.
// ============================================================================

namespace SimuladorGestorProcesos.Core
{
    /// <summary>
    /// Contrato para algoritmos de planificación de CPU.
    /// Cada implementación encapsula una política diferente para
    /// decidir el orden en que los procesos acceden al procesador.
    /// 
    /// Implementaciones disponibles:
    /// - <see cref="FCFSScheduler"/>: First Come, First Served (FIFO)
    /// - <see cref="RoundRobinScheduler"/>: Round Robin con quantum configurable
    /// - <see cref="SJFScheduler"/>: Shortest Job First (non-preemptive)
    /// - <see cref="PriorityScheduler"/>: Prioridades (menor número = mayor prioridad)
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Nombre descriptivo del algoritmo de planificación.
        /// Útil para logs y visualización en consola.
        /// </summary>
        string NombreAlgoritmo { get; }

        /// <summary>
        /// Añade un proceso a la cola de listos del planificador.
        /// El proceso debe estar en estado 'Listo' para ser aceptado.
        /// </summary>
        /// <param name="process">El PCB del proceso a encolar.</param>
        void AddProcess(PCB process);

        /// <summary>
        /// Selecciona y retorna el siguiente proceso que debe ejecutarse
        /// en la CPU, según la política del algoritmo implementado.
        /// El proceso se retira de la cola de listos.
        /// </summary>
        /// <returns>
        /// El PCB del proceso seleccionado, o <c>null</c> si la cola
        /// de listos está vacía.
        /// </returns>
        PCB? GetNextProcess();

        /// <summary>
        /// Simula un ciclo de reloj (tick) sobre el proceso que está
        /// actualmente en la CPU. Decrementa su tiempo restante y
        /// aplica la lógica específica del algoritmo (e.g., control
        /// de quantum en Round Robin).
        /// </summary>
        /// <param name="currentProcess">
        /// El proceso actualmente en ejecución en la CPU.
        /// </param>
        /// <returns>
        /// <c>true</c> si el proceso debe continuar en la CPU;
        /// <c>false</c> si debe ser desalojado (preemption) o ha terminado.
        /// </returns>
        bool Tick(PCB currentProcess);

        /// <summary>
        /// Indica cuántos procesos están esperando en la cola de listos.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Imprime en consola el estado actual de la cola de listos.
        /// </summary>
        void PrintQueue();

        /// <summary>
        /// Elimina un proceso de la cola de listos por su PID.
        /// Se utiliza al terminar forzosamente un proceso para
        /// evitar que sea despachado después de muerto.
        /// </summary>
        /// <param name="pid">El PID del proceso a remover.</param>
        /// <returns><c>true</c> si se encontró y removió; <c>false</c> si no estaba.</returns>
        bool RemoveProcess(int pid);
    }
}
