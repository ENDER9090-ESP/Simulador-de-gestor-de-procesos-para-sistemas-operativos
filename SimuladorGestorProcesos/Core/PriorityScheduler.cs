// ============================================================================
// Archivo: PriorityScheduler.cs
// Descripción: Planificador por Prioridades — No expropiativo.
//              Selecciona el proceso con la MAYOR prioridad de la cola.
//
// ╔══════════════════════════════════════════════════════════════════════╗
// ║  CONVENCIÓN DE PRIORIDAD:                                          ║
// ║  ► Número MÁS BAJO  = Prioridad MÁS ALTA  (ej. 0 = máxima)       ║
// ║  ► Número MÁS ALTO  = Prioridad MÁS BAJA  (ej. 10 = mínima)      ║
// ║                                                                    ║
// ║  Esto sigue la convención estándar de sistemas operativos reales   ║
// ║  como UNIX/Linux, donde el proceso con prioridad 0 se ejecuta      ║
// ║  antes que uno con prioridad 10.                                   ║
// ╚══════════════════════════════════════════════════════════════════════╝
//
// Nota técnica:
//   - Versión NON-PREEMPTIVE: una vez asignada la CPU, el proceso
//     se ejecuta hasta completarse sin ser desalojado.
//   - GetNextProcess() ordena por la propiedad Prioridad (ascendente),
//     seleccionando el PCB con el menor valor numérico.
//   - En caso de empate en Prioridad, se aplica desempate FCFS
//     (orden de inserción) gracias a la estabilidad de OrderBy en .NET.
//   - Se utiliza LINQ OrderBy para un ordenamiento claro y eficiente.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorGestorProcesos.Core
{
    public class PriorityScheduler : IScheduler
    {
        // Se usa List<PCB> en lugar de Queue<PCB> para poder ordenar
        // dinámicamente por Prioridad en cada llamada a GetNextProcess().
        private readonly List<PCB> _colaListos;
        private readonly EventLogger _logger;

        public string NombreAlgoritmo => "Prioridades (Menor número = Mayor prioridad)";
        public int Count => _colaListos.Count;

        public PriorityScheduler(EventLogger logger)
        {
            _colaListos = new List<PCB>();
            _logger = logger;
            _logger.AddLog($"Algoritmo configurado: {NombreAlgoritmo}", "SCHEDULER");
        }

        public void AddProcess(PCB process)
        {
            _colaListos.Add(process);
            _logger.AddLog(
                $"PID {process.PID:D3} añadido a cola de listos " +
                $"(Prioridad: {process.Prioridad}, total en cola: {_colaListos.Count}).",
                "SCHEDULER");
        }

        /// <summary>
        /// Selecciona el proceso con el menor valor numérico de Prioridad
        /// (= mayor prioridad real). En caso de empate, respeta el orden
        /// de llegada (FCFS) gracias a la estabilidad de OrderBy.
        /// El proceso seleccionado se retira de la cola.
        /// </summary>
        public PCB? GetNextProcess()
        {
            if (_colaListos.Count == 0) return null;

            // OrderBy ascendente: prioridad 0 (máxima) se selecciona primero.
            // La estabilidad de LINQ garantiza desempate FCFS.
            PCB highestPriority = _colaListos.OrderBy(p => p.Prioridad).First();
            _colaListos.Remove(highestPriority);

            _logger.AddLog(
                $"PID {highestPriority.PID:D3} seleccionado (Prioridad: {highestPriority.Prioridad}). " +
                $"Restantes: {_colaListos.Count}.",
                "SCHEDULER");

            return highestPriority;
        }

        /// <summary>
        /// Non-preemptive: el proceso continúa en CPU hasta que su
        /// RemainingTime llegue a 0. No hay desalojo.
        /// </summary>
        public bool Tick(PCB currentProcess)
        {
            currentProcess.DecrementarTiempo();
            return currentProcess.RemainingTime > 0;
        }

        public bool RemoveProcess(int pid)
        {
            int removed = _colaListos.RemoveAll(p => p.PID == pid);
            if (removed > 0)
                _logger.AddLog($"PID {pid:D3} removido de cola de listos.", "SCHEDULER");
            return removed > 0;
        }

        public void PrintQueue() { }
    }
}
