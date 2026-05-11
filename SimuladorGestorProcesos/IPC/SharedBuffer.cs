// ============================================================================
// Archivo: SharedBuffer.cs
// Descripción: Buffer compartido limitado — usa EventLogger.
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using SimuladorGestorProcesos.Core;

namespace SimuladorGestorProcesos.IPC
{
    public class SharedBuffer
    {
        private readonly Queue<string> _buffer;
        private readonly int _capacity;
        private readonly IScheduler _scheduler;
        private readonly EventLogger _logger;
        private readonly Queue<PCB> _blockedProducers;
        private readonly Queue<PCB> _blockedConsumers;
        private readonly SimulatedMutex _mutex;

        public int Count => _buffer.Count;
        public int Capacity => _capacity;
        public bool IsFull => _buffer.Count >= _capacity;
        public bool IsEmpty => _buffer.Count == 0;

        /// <summary>Contenido actual del buffer (para la API).</summary>
        public string[] Contents => _buffer.ToArray();

        public SharedBuffer(int capacity, IScheduler scheduler, EventLogger logger)
        {
            _capacity = capacity;
            _buffer = new Queue<string>();
            _blockedProducers = new Queue<PCB>();
            _blockedConsumers = new Queue<PCB>();
            _scheduler = scheduler;
            _logger = logger;
            _mutex = new SimulatedMutex("buffer", scheduler, logger);
            _logger.AddLog($"Buffer inicializado (capacidad: {capacity}).", "BUFFER");
        }

        public bool Produce(PCB producer, string item)
        {
            if (IsFull)
            {
                producer.CambiarEstado(ProcessState.Esperando);
                _blockedProducers.Enqueue(producer);
                _logger.AddLog(
                    $"PID {producer.PID:D3} (Productor): Buffer LLENO ({Count}/{_capacity}). BLOQUEADO.",
                    "BUFFER");
                return false;
            }

            _buffer.Enqueue(item);
            _logger.AddLog(
                $"PID {producer.PID:D3} (Productor): Produjo \"{item}\". Buffer [{Count}/{_capacity}].",
                "BUFFER");
            WakeConsumer();
            return true;
        }

        public string? Consume(PCB consumer)
        {
            if (IsEmpty)
            {
                consumer.CambiarEstado(ProcessState.Esperando);
                _blockedConsumers.Enqueue(consumer);
                _logger.AddLog(
                    $"PID {consumer.PID:D3} (Consumidor): Buffer VACÍO ({Count}/{_capacity}). BLOQUEADO.",
                    "BUFFER");
                return null;
            }

            string item = _buffer.Dequeue();
            _logger.AddLog(
                $"PID {consumer.PID:D3} (Consumidor): Consumió \"{item}\". Buffer [{Count}/{_capacity}].",
                "BUFFER");
            WakeProducer();
            return item;
        }

        private void WakeConsumer()
        {
            if (_blockedConsumers.Count > 0)
            {
                PCB c = _blockedConsumers.Dequeue();
                c.CambiarEstado(ProcessState.Listo);
                _scheduler.AddProcess(c);
                _logger.AddLog(
                    $"Consumidor PID {c.PID:D3} despertado → Listo.", "BUFFER");
            }
        }

        private void WakeProducer()
        {
            if (_blockedProducers.Count > 0)
            {
                PCB p = _blockedProducers.Dequeue();
                p.CambiarEstado(ProcessState.Listo);
                _scheduler.AddProcess(p);
                _logger.AddLog(
                    $"Productor PID {p.PID:D3} despertado → Listo.", "BUFFER");
            }
        }

        public bool RemoveFromQueues(int pid)
        {
            bool removed = false;
            int c;

            c = _blockedProducers.Count;
            var tp = new Queue<PCB>();
            while (_blockedProducers.Count > 0)
            { PCB p = _blockedProducers.Dequeue(); if (p.PID != pid) tp.Enqueue(p); }
            while (tp.Count > 0) _blockedProducers.Enqueue(tp.Dequeue());
            if (_blockedProducers.Count < c) removed = true;

            c = _blockedConsumers.Count;
            var tc = new Queue<PCB>();
            while (_blockedConsumers.Count > 0)
            { PCB p = _blockedConsumers.Dequeue(); if (p.PID != pid) tc.Enqueue(p); }
            while (tc.Count > 0) _blockedConsumers.Enqueue(tc.Dequeue());
            if (_blockedConsumers.Count < c) removed = true;

            if (removed)
                _logger.AddLog($"PID {pid:D3} removido de colas de bloqueo del buffer.", "BUFFER");
            return removed;
        }

        public void PrintStatus() { }
    }
}
