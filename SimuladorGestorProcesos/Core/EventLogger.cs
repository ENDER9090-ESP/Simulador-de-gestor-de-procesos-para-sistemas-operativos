// ============================================================================
// Archivo: EventLogger.cs
// Descripción: Servicio Singleton de logging centralizado. Reemplaza los
//              Console.WriteLine dispersos. Almacena logs en memoria para
//              que el frontend web los consuma vía /api/status.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorGestorProcesos.Core
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = "";
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Logger centralizado que almacena eventos del simulador en memoria.
    /// Registrado como Singleton en DI para compartir estado global.
    /// También expone una referencia estática (Current) para clases que
    /// no pueden recibir inyección de dependencias (ej. PCB).
    /// </summary>
    public class EventLogger
    {
        private readonly List<LogEntry> _logs = new();
        private readonly object _lock = new();
        private const int MAX_LOGS = 500;

        /// <summary>
        /// Referencia estática al logger activo. Configurada al iniciar
        /// la aplicación. Permite a clases como PCB loggear sin DI.
        /// </summary>
        public static EventLogger? Current { get; set; }

        public void AddLog(string message, string category = "SYSTEM")
        {
            lock (_lock)
            {
                _logs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    Category = category
                });

                if (_logs.Count > MAX_LOGS)
                    _logs.RemoveAt(0);
            }
        }

        public List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (_lock)
            {
                return _logs.TakeLast(count).ToList();
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        public int Count
        {
            get { lock (_lock) { return _logs.Count; } }
        }
    }
}
