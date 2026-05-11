// ============================================================================
// Archivo: PCB.cs (Process Control Block)
// Descripción: Estructura de datos que representa un proceso del sistema.
// ============================================================================

using System;
using System.Collections.Generic;

namespace SimuladorGestorProcesos.Core
{
    public class PCB
    {
        private static int _contadorPID = 0;

        public int PID { get; private set; }
        public ProcessState Estado { get; private set; }
        public int Prioridad { get; set; }
        public DateTime FechaCreacion { get; private set; }
        public int MemoriaRequeridaMB { get; private set; }
        public int BurstTime { get; private set; }
        public int RemainingTime { get; private set; }
        public List<Resource> RecursosAsignados { get; private set; }
        public Action? TareaCPU { get; set; }
        public TerminationReason? ExitReason { get; set; }

        public PCB(int prioridad, int memoriaMB = 128, int burstTime = 100)
        {
            PID = ++_contadorPID;
            Estado = ProcessState.Nuevo;
            Prioridad = prioridad;
            MemoriaRequeridaMB = memoriaMB;
            BurstTime = burstTime;
            RemainingTime = burstTime;
            FechaCreacion = DateTime.Now;
            RecursosAsignados = new List<Resource>();
        }

        public bool CambiarEstado(ProcessState nuevoEstado)
        {
            if (!EsTransicionValida(nuevoEstado))
            {
                EventLogger.Current?.AddLog(
                    $"Transición inválida: PID {PID} no puede pasar de '{Estado}' a '{nuevoEstado}'.",
                    "ERROR");
                return false;
            }

            ProcessState estadoAnterior = Estado;
            Estado = nuevoEstado;
            EventLogger.Current?.AddLog(
                $"PID {PID}: {estadoAnterior} → {Estado}", "TRANSICIÓN");
            return true;
        }

        public void AsignarRecurso(Resource recurso)
        {
            RecursosAsignados.Add(recurso);
        }

        public void LiberarRecursos()
        {
            RecursosAsignados.Clear();
        }

        public void DecrementarTiempo()
        {
            if (RemainingTime > 0)
                RemainingTime--;
        }

        private bool EsTransicionValida(ProcessState nuevoEstado)
        {
            return (Estado, nuevoEstado) switch
            {
                (ProcessState.Nuevo, ProcessState.Listo) => true,
                (ProcessState.Listo, ProcessState.Ejecutando) => true,
                (ProcessState.Ejecutando, ProcessState.Listo) => true,
                (ProcessState.Ejecutando, ProcessState.Esperando) => true,
                (ProcessState.Ejecutando, ProcessState.Terminado) => true,
                (ProcessState.Esperando, ProcessState.Listo) => true,
                (_, ProcessState.Terminado) => true,
                _ => false
            };
        }

        public override string ToString()
        {
            string info = $"[PID: {PID:D3}] Estado: {Estado,-12} | " +
                   $"Pri: {Prioridad} | RAM: {MemoriaRequeridaMB,5} MB | " +
                   $"CPU: {RemainingTime,3}/{BurstTime}ms";
            if (ExitReason.HasValue)
                info += $" | Salida: {ExitReason.Value}";
            return info;
        }

        public static void ReiniciarContadorPID()
        {
            _contadorPID = 0;
        }
    }
}
