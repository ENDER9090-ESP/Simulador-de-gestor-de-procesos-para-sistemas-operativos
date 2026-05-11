// ============================================================================
// Archivo: FCFSScheduler.cs
// Descripción: Planificador FCFS (First Come, First Served) — FIFO puro.
// ============================================================================

using System;
using System.Collections.Generic;

namespace SimuladorGestorProcesos.Core
{
    public class FCFSScheduler : IScheduler
    {
        private readonly Queue<PCB> _colaListos;
        private readonly EventLogger _logger;

        public string NombreAlgoritmo => "FCFS (First Come, First Served)";
        public int Count => _colaListos.Count;

        public FCFSScheduler(EventLogger logger)
        {
            _colaListos = new Queue<PCB>();
            _logger = logger;
            _logger.AddLog($"Algoritmo configurado: {NombreAlgoritmo}", "SCHEDULER");
        }

        public void AddProcess(PCB process)
        {
            _colaListos.Enqueue(process);
            _logger.AddLog(
                $"PID {process.PID:D3} añadido a cola de listos (pos: {_colaListos.Count}).",
                "SCHEDULER");
        }

        public PCB? GetNextProcess()
        {
            if (_colaListos.Count == 0) return null;
            PCB siguiente = _colaListos.Dequeue();
            _logger.AddLog(
                $"PID {siguiente.PID:D3} seleccionado (FCFS). Restantes: {_colaListos.Count}.",
                "SCHEDULER");
            return siguiente;
        }

        public bool Tick(PCB currentProcess)
        {
            currentProcess.DecrementarTiempo();
            return currentProcess.RemainingTime > 0;
        }

        public bool RemoveProcess(int pid)
        {
            int originalCount = _colaListos.Count;
            var temp = new Queue<PCB>();
            while (_colaListos.Count > 0)
            {
                PCB p = _colaListos.Dequeue();
                if (p.PID != pid) temp.Enqueue(p);
            }
            while (temp.Count > 0) _colaListos.Enqueue(temp.Dequeue());
            bool removed = _colaListos.Count < originalCount;
            if (removed)
                _logger.AddLog($"PID {pid:D3} removido de cola de listos.", "SCHEDULER");
            return removed;
        }

        public void PrintQueue() { }
    }
}
