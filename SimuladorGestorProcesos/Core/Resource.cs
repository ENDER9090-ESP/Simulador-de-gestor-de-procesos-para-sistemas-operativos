// ============================================================================
// Archivo: Resource.cs
// Descripción: Clase temporal que modela un recurso del sistema.
//              Diseñada como punto de extensión para futuras implementaciones
//              de gestión de recursos (CPU, memoria, dispositivos de E/S).
// ============================================================================

namespace SimuladorGestorProcesos.Core
{
    /// <summary>
    /// Representa un recurso del sistema operativo que puede ser asignado
    /// a un proceso. Esta es una implementación base que será extendida
    /// conforme se agreguen módulos de gestión de recursos.
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// Identificador único del recurso.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nombre descriptivo del recurso (e.g., "CPU-0", "Disco-1", "Impresora").
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Tipo de recurso (e.g., "CPU", "Memoria", "E/S").
        /// </summary>
        public string Tipo { get; set; }

        public Resource(string id, string nombre, string tipo)
        {
            Id = id;
            Nombre = nombre;
            Tipo = tipo;
        }

        public override string ToString()
        {
            return $"[{Tipo}] {Nombre} (ID: {Id})";
        }
    }
}
