// ============================================================================
// Archivo: SimulatedMutex.cs
// Descripción: Mutex simulado para exclusión mutua — usa EventLogger.
// ============================================================================

using System.Collections.Generic;
using SimuladorGestorProcesos.Core;

namespace SimuladorGestorProcesos.IPC
{
    public class SimulatedMutex
    {
        private readonly string _nombre;
        private readonly IScheduler _scheduler;
        private readonly EventLogger _logger;
        private readonly Queue<PCB> _colaBloqueados;

        public PCB? Owner { get; private set; }
        public bool IsLocked => Owner != null;
        public int BlockedCount => _colaBloqueados.Count;

        public SimulatedMutex(string nombre, IScheduler scheduler, EventLogger logger)
        {
            _nombre = nombre;
            _scheduler = scheduler;
            _logger = logger;
            _colaBloqueados = new Queue<PCB>();
        }

        public bool Acquire(PCB process)
        {
            if (!IsLocked)
            {
                Owner = process;
                _logger.AddLog($"PID {process.PID:D3} adquirió lock [{_nombre}].", "MUTEX");
                return true;
            }

            process.CambiarEstado(ProcessState.Esperando);
            _colaBloqueados.Enqueue(process);
            _logger.AddLog(
                $"PID {process.PID:D3} BLOQUEADO en [{_nombre}] " +
                $"(dueño: PID {Owner?.PID:D3}). Cola: {_colaBloqueados.Count}.", "MUTEX");
            return false;
        }

        public void Release(PCB process)
        {
            if (Owner?.PID != process.PID) return;

            if (_colaBloqueados.Count > 0)
            {
                PCB despertado = _colaBloqueados.Dequeue();
                Owner = despertado;
                despertado.CambiarEstado(ProcessState.Listo);
                _scheduler.AddProcess(despertado);
                _logger.AddLog(
                    $"Lock [{_nombre}] transferido a PID {despertado.PID:D3}.", "MUTEX");
            }
            else
            {
                Owner = null;
                _logger.AddLog($"PID {process.PID:D3} liberó lock [{_nombre}].", "MUTEX");
            }
        }

        public void ForceRelease(int pid)
        {
            if (Owner?.PID != pid) return;
            _logger.AddLog($"Lock [{_nombre}] FORZADO (PID {pid:D3} terminado).", "MUTEX");

            if (_colaBloqueados.Count > 0)
            {
                PCB despertado = _colaBloqueados.Dequeue();
                Owner = despertado;
                despertado.CambiarEstado(ProcessState.Listo);
                _scheduler.AddProcess(despertado);
                _logger.AddLog(
                    $"Lock [{_nombre}] transferido a PID {despertado.PID:D3}.", "MUTEX");
            }
            else
            {
                Owner = null;
            }
        }

        public bool RemoveFromBlockedQueue(int pid)
        {
            int orig = _colaBloqueados.Count;
            var temp = new Queue<PCB>();
            while (_colaBloqueados.Count > 0)
            {
                PCB p = _colaBloqueados.Dequeue();
                if (p.PID != pid) temp.Enqueue(p);
            }
            while (temp.Count > 0) _colaBloqueados.Enqueue(temp.Dequeue());
            bool removed = _colaBloqueados.Count < orig;
            if (removed)
                _logger.AddLog($"PID {pid:D3} removido de bloqueados [{_nombre}].", "MUTEX");
            return removed;
        }
    }
}
