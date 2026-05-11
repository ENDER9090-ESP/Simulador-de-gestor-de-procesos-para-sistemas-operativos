// ============================================================================
// Archivo: ProcessManager.cs
// Descripción: Controlador principal del ciclo de vida de procesos.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using SimuladorGestorProcesos.IPC;

namespace SimuladorGestorProcesos.Core
{
    public class ProcessManager
    {
        private readonly List<PCB> _tablaProcesos;
        private readonly ResourceManager _resourceManager;
        private IScheduler _scheduler;
        private readonly EventLogger _logger;
        private readonly List<SimulatedMutex> _registeredMutexes;
        private readonly List<SharedBuffer> _registeredBuffers;

        public PCB? CurrentProcess { get; private set; }
        public int CicloActual { get; private set; }
        public IScheduler Scheduler => _scheduler;

        public IReadOnlyList<PCB> TablaProcesos => _tablaProcesos.AsReadOnly();
        public int TotalProcesos => _tablaProcesos.Count;
        public int ProcesosActivos =>
            _tablaProcesos.Count(p => p.Estado != ProcessState.Terminado);

        public ProcessManager(ResourceManager resourceManager, IScheduler scheduler, EventLogger logger)
        {
            _tablaProcesos = new List<PCB>();
            _resourceManager = resourceManager;
            _scheduler = scheduler;
            _logger = logger;
            CurrentProcess = null;
            CicloActual = 0;
            _registeredMutexes = new List<SimulatedMutex>();
            _registeredBuffers = new List<SharedBuffer>();
        }

        public void RegisterMutex(SimulatedMutex mutex) => _registeredMutexes.Add(mutex);
        public void RegisterBuffer(SharedBuffer buffer) => _registeredBuffers.Add(buffer);

        /// <summary>
        /// Cambia dinámicamente el algoritmo de planificación.
        /// Migra los procesos en cola de listos al nuevo scheduler.
        /// </summary>
        public void ChangeScheduler(IScheduler newScheduler)
        {
            // Recoger procesos en estado Listo de la tabla (fuente de verdad)
            var listos = _tablaProcesos
                .Where(p => p.Estado == ProcessState.Listo)
                .ToList();

            // Si hay un proceso ejecutándose, lo desalojamos y lo ponemos en Listo
            if (CurrentProcess != null && CurrentProcess.Estado == ProcessState.Ejecutando)
            {
                CurrentProcess.CambiarEstado(ProcessState.Listo);
                if (!listos.Contains(CurrentProcess))
                    listos.Add(CurrentProcess);
                CurrentProcess = null;
            }

            // Cambiar al nuevo scheduler
            _scheduler = newScheduler;

            // Re-encolar todos los procesos listos
            foreach (var p in listos)
                _scheduler.AddProcess(p);

            _logger.AddLog(
                $"Scheduler cambiado a: {newScheduler.NombreAlgoritmo}. " +
                $"{listos.Count} procesos migrados.",
                "SCHEDULER");
        }

        public PCB? CreateProcess(int prioridad, int memoriaMB = 128, int burstTime = 100)
        {
            prioridad = Math.Clamp(prioridad, 0, 10);
            PCB nuevoProceso = new PCB(prioridad, memoriaMB, burstTime);
            _tablaProcesos.Add(nuevoProceso);

            _logger.AddLog(
                $"PID {nuevoProceso.PID:D3} creado (Pri: {prioridad}, " +
                $"RAM: {memoriaMB} MB, Burst: {burstTime}ms).", "PROCESO");

            bool ramAsignada = _resourceManager.RequestRAM(nuevoProceso, memoriaMB);
            if (!ramAsignada)
            {
                _logger.AddLog(
                    $"PID {nuevoProceso.PID:D3}: RECHAZADO por falta de RAM.", "ERROR");
                return null;
            }

            nuevoProceso.CambiarEstado(ProcessState.Listo);
            _scheduler.AddProcess(nuevoProceso);
            return nuevoProceso;
        }

        public bool SuspendProcess(int pid)
        {
            PCB? proceso = BuscarProceso(pid);
            if (proceso == null) return false;
            bool exito = proceso.CambiarEstado(ProcessState.Esperando);
            if (exito)
            {
                if (CurrentProcess?.PID == pid)
                    CurrentProcess = null;
                _scheduler.RemoveProcess(pid);
                _logger.AddLog($"PID {pid:D3} suspendido.", "PROCESO");
            }
            return exito;
        }

        public bool ResumeProcess(int pid)
        {
            PCB? proceso = BuscarProceso(pid);
            if (proceso == null) return false;
            bool exito = proceso.CambiarEstado(ProcessState.Listo);
            if (exito)
            {
                _scheduler.AddProcess(proceso);
                _logger.AddLog($"PID {pid:D3} reanudado → Listo.", "PROCESO");
            }
            return exito;
        }

        public bool TerminateProcess(int pid, TerminationReason reason = TerminationReason.Normal)
        {
            PCB? proceso = BuscarProceso(pid);
            if (proceso == null) return false;

            if (proceso.Estado == ProcessState.Terminado)
            {
                _logger.AddLog($"PID {pid:D3} ya está terminado.", "AVISO");
                return false;
            }

            int ramRecuperada = _resourceManager.GetRAMByPID(pid);
            ProcessState estadoPrevio = proceso.Estado;

            _logger.AddLog(
                $"═══ TERMINANDO PID {pid:D3} | Causa: {reason} | " +
                $"Estado previo: {estadoPrevio} ═══", "TERMINACIÓN");

            proceso.CambiarEstado(ProcessState.Terminado);
            proceso.ExitReason = reason;

            if (CurrentProcess?.PID == pid)
            {
                CurrentProcess = null;
                _logger.AddLog($"PID {pid:D3}: CPU liberada.", "CPU");
            }

            _resourceManager.ReleaseRAM(proceso);

            foreach (var mutex in _registeredMutexes)
            {
                mutex.ForceRelease(pid);
                mutex.RemoveFromBlockedQueue(pid);
            }

            foreach (var buffer in _registeredBuffers)
                buffer.RemoveFromQueues(pid);

            _scheduler.RemoveProcess(pid);
            proceso.LiberarRecursos();
            proceso.TareaCPU = null;

            _logger.AddLog(
                $"PID {pid:D3} finalizado. Causa: {reason}. " +
                $"RAM recuperada: {ramRecuperada} MB.", "TERMINACIÓN");
            return true;
        }

        public bool RunCycle()
        {
            CicloActual++;
            _logger.AddLog($"── Ciclo {CicloActual:D4} ──", "CPU");

            if (CurrentProcess == null)
            {
                CurrentProcess = _scheduler.GetNextProcess();
                if (CurrentProcess == null)
                {
                    _logger.AddLog("CPU Ociosa. No hay procesos en cola.", "CPU");
                    return false;
                }
                CurrentProcess.CambiarEstado(ProcessState.Ejecutando);
            }

            if (CurrentProcess.TareaCPU != null)
            {
                CurrentProcess.TareaCPU.Invoke();
                if (CurrentProcess.Estado != ProcessState.Ejecutando)
                {
                    _logger.AddLog(
                        $"PID {CurrentProcess.PID:D3} bloqueado por recurso IPC. CPU liberada.",
                        "CPU");
                    CurrentProcess = null;
                    return true;
                }
            }

            _logger.AddLog(
                $"Ejecutando PID {CurrentProcess.PID:D3} " +
                $"(Restante: {CurrentProcess.RemainingTime}ms)", "CPU");

            bool continuar = _scheduler.Tick(CurrentProcess);

            if (!continuar)
            {
                if (CurrentProcess.RemainingTime <= 0)
                {
                    TerminateProcess(CurrentProcess.PID, TerminationReason.Normal);
                }
                else
                {
                    CurrentProcess.CambiarEstado(ProcessState.Listo);
                    _scheduler.AddProcess(CurrentProcess);
                    CurrentProcess = null;
                }
            }

            return true;
        }

        public PCB? BuscarProceso(int pid)
        {
            return _tablaProcesos.FirstOrDefault(p => p.PID == pid);
        }

        public List<PCB> ObtenerProcesosPorEstado(ProcessState estado)
        {
            return _tablaProcesos.Where(p => p.Estado == estado).ToList();
        }
    }
}
