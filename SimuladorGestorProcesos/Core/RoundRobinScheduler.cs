// ============================================================================
// Archivo: RoundRobinScheduler.cs
// Descripción: Planificador Round Robin con quantum configurable.
// ============================================================================

using System;
using System.Collections.Generic;

namespace SimuladorGestorProcesos.Core
{
    public class RoundRobinScheduler : IScheduler
    {
        private readonly Queue<PCB> _colaListos;
        private readonly EventLogger _logger;
        public int Quantum { get; private set; }
        private int _ticksEnQuantumActual;

        public string NombreAlgoritmo => $"Round Robin (Quantum: {Quantum}ms)";
        public int Count => _colaListos.Count;

        public RoundRobinScheduler(int quantum, EventLogger logger)
        {
            if (quantum <= 0)
                throw new ArgumentException("El quantum debe ser positivo.", nameof(quantum));

            Quantum = quantum;
            _ticksEnQuantumActual = 0;
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
            _ticksEnQuantumActual = 0;
            _logger.AddLog(
                $"PID {siguiente.PID:D3} seleccionado (RR, quantum: {Quantum}ms). " +
                $"Restantes: {_colaListos.Count}.",
                "SCHEDULER");
            return siguiente;
        }

        public bool Tick(PCB currentProcess)
        {
            currentProcess.DecrementarTiempo();
            _ticksEnQuantumActual++;

            if (currentProcess.RemainingTime <= 0)
            {
                _logger.AddLog(
                    $"PID {currentProcess.PID:D3}: Ejecución completada (RemainingTime = 0).",
                    "SCHEDULER");
                return false;
            }

            if (_ticksEnQuantumActual >= Quantum)
            {
                _logger.AddLog(
                    $"PID {currentProcess.PID:D3}: Quantum agotado ({Quantum}ms). " +
                    $"Desalojando → reinsertando (Restante: {currentProcess.RemainingTime}ms).",
                    "SCHEDULER");
                _ticksEnQuantumActual = 0;
                return false;
            }

            return true;
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
