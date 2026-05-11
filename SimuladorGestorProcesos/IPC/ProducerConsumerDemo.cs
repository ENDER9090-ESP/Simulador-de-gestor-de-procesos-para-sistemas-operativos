// ============================================================================
// Archivo: ProducerConsumerDemo.cs
// Descripción: Orquestador demo Productor-Consumidor — usa EventLogger.
// ============================================================================

using SimuladorGestorProcesos.Core;

namespace SimuladorGestorProcesos.IPC
{
    public class ProducerConsumerDemo
    {
        private SharedBuffer? _buffer;
        private int _itemCounter;
        private int _producerPID;
        private int _consumerPID;

        public SharedBuffer? Buffer => _buffer;

        public void SetupDemo(
            ProcessManager manager,
            IScheduler scheduler,
            EventLogger logger,
            int bufferSize = 5,
            int producerBurst = 8,
            int consumerBurst = 8)
        {
            _itemCounter = 0;
            _buffer = new SharedBuffer(bufferSize, scheduler, logger);
            manager.RegisterBuffer(_buffer);

            logger.AddLog(
                $"Configurando demo Productor-Consumidor (buffer: {bufferSize}).",
                "DEMO");

            PCB? producer = manager.CreateProcess(
                prioridad: 1, memoriaMB: 128, burstTime: producerBurst);

            if (producer != null)
            {
                _producerPID = producer.PID;
                producer.TareaCPU = () =>
                {
                    _itemCounter++;
                    string dato = $"Dato-{_itemCounter:D2}";
                    _buffer.Produce(producer, dato);
                };
            }

            PCB? consumer = manager.CreateProcess(
                prioridad: 2, memoriaMB: 128, burstTime: consumerBurst);

            if (consumer != null)
            {
                _consumerPID = consumer.PID;
                consumer.TareaCPU = () =>
                {
                    _buffer.Consume(consumer);
                };
            }

            logger.AddLog(
                $"Demo configurada: Productor PID {_producerPID:D3} | " +
                $"Consumidor PID {_consumerPID:D3} | Buffer [{bufferSize}].",
                "DEMO");
        }
    }
}
