// ============================================================================
// Archivo: SJFScheduler.cs
// Descripción: Planificador SJF (Shortest Job First) — No expropiativo.
//              Selecciona siempre el proceso con el menor RemainingTime
//              de la colección de procesos listos.
//
// Nota técnica:
//   - Versión NON-PREEMPTIVE: una vez que un proceso obtiene la CPU,
//     se ejecuta hasta completarse (no se desaloja por uno más corto).
//   - GetNextProcess() evalúa TODOS los procesos en cola y retorna
//     el que tenga el menor valor en RemainingTime.
//   - En caso de empate en RemainingTime, se selecciona el que fue
//     añadido primero (orden de inserción — FCFS como desempate).
//   - Se utiliza LINQ OrderBy para un ordenamiento claro y eficiente.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorGestorProcesos.Core
{
    public class SJFScheduler : IScheduler
    {
        // Se usa List<PCB> en lugar de Queue<PCB> para poder ordenar
        // dinámicamente por RemainingTime en cada llamada a GetNextProcess().
        private readonly List<PCB> _colaListos;
        private readonly EventLogger _logger;

        public string NombreAlgoritmo => "SJF (Shortest Job First)";
        public int Count => _colaListos.Count;

        public SJFScheduler(EventLogger logger)
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
                $"(RemainingTime: {process.RemainingTime}ms, total en cola: {_colaListos.Count}).",
                "SCHEDULER");
        }

        /// <summary>
        /// Selecciona el proceso con el menor RemainingTime de la cola.
        /// En caso de empate, se respeta el orden de llegada (FCFS).
        /// El proceso seleccionado se retira de la cola.
        /// </summary>
        public PCB? GetNextProcess()
        {
            if (_colaListos.Count == 0) return null;

            // OrderBy es estable en .NET: elementos iguales mantienen
            // su orden relativo original → desempate natural por FCFS.
            PCB shortest = _colaListos.OrderBy(p => p.RemainingTime).First();
            _colaListos.Remove(shortest);

            _logger.AddLog(
                $"PID {shortest.PID:D3} seleccionado (SJF, RemainingTime: {shortest.RemainingTime}ms). " +
                $"Restantes: {_colaListos.Count}.",
                "SCHEDULER");

            return shortest;
        }

        /// <summary>
        /// Non-preemptive: el proceso continúa en CPU hasta que su
        /// RemainingTime llegue a 0. No hay desalojo por quantum.
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
