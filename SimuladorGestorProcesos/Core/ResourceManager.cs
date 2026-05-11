// ============================================================================
// Archivo: ResourceManager.cs
// Descripción: Gestor central de recursos (RAM) del sistema simulado.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorGestorProcesos.Core
{
    public class ResourceManager
    {
        public const int TOTAL_RAM_MB = 4096;
        public const int TOTAL_CPU_CORES = 1;

        public int AvailableRAM { get; private set; }
        public int UsedRAM => TOTAL_RAM_MB - AvailableRAM;
        public double RAMUsagePercent =>
            Math.Round((double)UsedRAM / TOTAL_RAM_MB * 100, 1);

        private readonly Dictionary<int, int> _asignacionesPorPID;
        private readonly EventLogger _logger;

        public ResourceManager(EventLogger logger)
        {
            AvailableRAM = TOTAL_RAM_MB;
            _asignacionesPorPID = new Dictionary<int, int>();
            _logger = logger;
            _logger.AddLog(
                $"ResourceManager inicializado: {TOTAL_RAM_MB} MB RAM | {TOTAL_CPU_CORES} CPU core(s)",
                "RECURSO");
        }

        public bool RequestRAM(PCB process, int amountMB)
        {
            if (amountMB <= 0)
            {
                _logger.AddLog(
                    $"PID {process.PID:D3}: RAM solicitada debe ser > 0.", "ERROR");
                return false;
            }

            if (amountMB > AvailableRAM)
            {
                _logger.AddLog(
                    $"PID {process.PID:D3}: CONFLICTO — Requiere {amountMB} MB, " +
                    $"disponible: {AvailableRAM} MB.", "RECURSO");
                return false;
            }

            AvailableRAM -= amountMB;

            if (_asignacionesPorPID.ContainsKey(process.PID))
                _asignacionesPorPID[process.PID] += amountMB;
            else
                _asignacionesPorPID[process.PID] = amountMB;

            _logger.AddLog(
                $"PID {process.PID:D3}: {amountMB} MB asignados. " +
                $"RAM: {AvailableRAM}/{TOTAL_RAM_MB} MB ({RAMUsagePercent}% uso).",
                "RECURSO");
            return true;
        }

        public int ReleaseRAM(PCB process)
        {
            if (!_asignacionesPorPID.ContainsKey(process.PID))
                return 0;

            int memoriaLiberada = _asignacionesPorPID[process.PID];
            AvailableRAM += memoriaLiberada;
            _asignacionesPorPID.Remove(process.PID);

            _logger.AddLog(
                $"PID {process.PID:D3}: {memoriaLiberada} MB liberados. " +
                $"RAM: {AvailableRAM}/{TOTAL_RAM_MB} MB ({RAMUsagePercent}% uso).",
                "RECURSO");
            return memoriaLiberada;
        }

        public int GetRAMByPID(int pid)
        {
            return _asignacionesPorPID.ContainsKey(pid)
                ? _asignacionesPorPID[pid] : 0;
        }

        public bool HasEnoughRAM(int amountMB) => amountMB <= AvailableRAM;

        /// <summary>Devuelve el desglose de RAM por PID (para la API).</summary>
        public Dictionary<int, int> GetRAMBreakdown()
        {
            return new Dictionary<int, int>(_asignacionesPorPID);
        }
    }
}
