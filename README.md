# 🖥️ Simulador de Gestor de Procesos (OS Simulator)

> Simulador web interactivo desarrollado con arquitectura de Minimal APIs que modela el ciclo de vida de los procesos, 
gestión de memoria RAM, algoritmos de planificación de CPU y sincronización de concurrencia (IPC).

## Información Académica

* **Materia:** Sistemas Operativos
* **Institución:** Universidad Autónoma de Tamaulipas (UAT)
* **Semestre:** 6th
* **Profesor:** Muñoz Quintero Dante Adolfo

## Integrantes del Equipo

| Nombre | Rol / Contribución Principal |
| :--- | :--- |
| **Francisco Mogollón, Jose Antonio** | Arquitectura del núcleo (ASP.NET Core), diseño del Dashboard Web interactivo y motor de eventos (Logger). |
| **Flores Cabrera, Gerardo** | Implementación y alternancia dinámica de algoritmos de planificación de CPU (FCFS, SJF, RR, Prioridades). |
| **Mendoza Gómez, Gerardo Agustín** | Gestión centralizada de recursos (RAM) y módulos de Comunicación Interprocesos (IPC - Mutex y Buffers). |

---

## Características Principales

* **Ciclo de Vida de Procesos:** Simulación estricta del modelo clásico de 5 estados (Nuevo, Listo, Ejecutando, Esperando, Terminado) 
mediante bloques de control (PCB).
* **Gestor de Memoria:** Asignación y liberación de RAM centralizada para evitar fugas de memoria y manejar condiciones *Out-Of-Memory*.
* **Planificación Polimórfica:** Soporte para cambio en caliente (Hot-Swapping) entre algoritmos:
  * *FCFS (First-Come, First-Served)*
  * *SJF (Shortest Job First)*
  * *Round Robin (con Quantum configurable)*
  * *Planificación por Prioridades*
* **Concurrencia e IPC:** Demostración en tiempo real del problema del Productor-Consumidor utilizando semáforos lógicos y exclusión mutua (Mutex simulado).
* **Observabilidad:** Registro de eventos (Logs) trazable con milisegundos y visualización en un Dashboard Web sin bloqueos de hilos (Thread-safe).

## Tecnologías Utilizadas

* **Backend / Núcleo:** C# (.NET 8+ / ASP.NET Core Minimal APIs)
* **Frontend:** HTML5, CSS3, JavaScript (Vanilla)
* **Patrones de Diseño:** Singleton (Logger), Strategy (Algoritmos de CPU), Inyección de Dependencias.

---
*Proyecto desarrollado con fines académicos para comprender a profundidad el diseño e implementación interna de los Sistemas Operativos modernos.*
