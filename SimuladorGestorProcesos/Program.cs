// ============================================================================
// Archivo: Program.cs
// Descripción: Backend ASP.NET Core Minimal API para el Simulador.
//              Sirve archivos estáticos + endpoints REST para controlar
//              la simulación desde el dashboard web.
// ============================================================================

using SimuladorGestorProcesos.Core;
using SimuladorGestorProcesos.IPC;

var builder = WebApplication.CreateBuilder(args);

// ── Registrar servicios Singleton (estado global del simulador) ──────
builder.Services.AddSingleton<EventLogger>();
builder.Services.AddSingleton<ResourceManager>();
builder.Services.AddSingleton<IScheduler>(sp =>
    new FCFSScheduler(sp.GetRequiredService<EventLogger>()));
builder.Services.AddSingleton<ProcessManager>();

var app = builder.Build();

// ── Configurar el EventLogger estático para PCB ──────────────────────
var logger = app.Services.GetRequiredService<EventLogger>();
EventLogger.Current = logger;
logger.AddLog("Simulador de Gestor de Procesos iniciado.", "SYSTEM");

// ── Middleware ────────────────────────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();

// =====================================================================
// API Endpoints
// =====================================================================

// GET /api/status — Estado global completo del simulador
app.MapGet("/api/status", (ProcessManager pm, ResourceManager rm, EventLogger log) =>
{
    var sched = pm.Scheduler;
    var procesos = pm.TablaProcesos.Select(p => new
    {
        p.PID,
        Estado = p.Estado.ToString(),
        p.Prioridad,
        p.MemoriaRequeridaMB,
        p.BurstTime,
        p.RemainingTime,
        ExitReason = p.ExitReason?.ToString()
    }).ToList();

    return Results.Ok(new
    {
        Ciclo = pm.CicloActual,
        CPU = pm.CurrentProcess != null ? new
        {
            pm.CurrentProcess.PID,
            pm.CurrentProcess.RemainingTime,
            pm.CurrentProcess.BurstTime
        } : null,
        RAM = new
        {
            Total = ResourceManager.TOTAL_RAM_MB,
            rm.AvailableRAM,
            rm.UsedRAM,
            rm.RAMUsagePercent,
            Breakdown = rm.GetRAMBreakdown()
        },
        Scheduler = new
        {
            Algoritmo = sched.NombreAlgoritmo,
            EnCola = sched.Count
        },
        Procesos = procesos,
        ProcesosActivos = pm.ProcesosActivos,
        Logs = log.GetRecentLogs(80).Select(l => new
        {
            Hora = l.Timestamp.ToString("HH:mm:ss.fff"),
            l.Category,
            l.Message
        })
    });
});

// POST /api/process/create — Crear proceso manual
app.MapPost("/api/process/create", (ProcessManager pm, HttpRequest req) =>
{
    int prioridad = 5, memoria = 128, burst = 100;

    if (req.Query.ContainsKey("prioridad"))
        int.TryParse(req.Query["prioridad"], out prioridad);
    if (req.Query.ContainsKey("memoria"))
        int.TryParse(req.Query["memoria"], out memoria);
    if (req.Query.ContainsKey("burst"))
        int.TryParse(req.Query["burst"], out burst);

    var proceso = pm.CreateProcess(prioridad, memoria, burst);
    if (proceso == null)
        return Results.BadRequest(new { error = "No se pudo crear: RAM insuficiente." });

    return Results.Ok(new { pid = proceso.PID, message = $"Proceso PID {proceso.PID} creado." });
});

// POST /api/process/{pid}/kill — Terminar proceso forzosamente
app.MapPost("/api/process/{pid}/kill", (int pid, ProcessManager pm) =>
{
    bool ok = pm.TerminateProcess(pid, TerminationReason.ForcedByUser);
    return ok
        ? Results.Ok(new { message = $"PID {pid} terminado (ForcedByUser)." })
        : Results.NotFound(new { error = $"PID {pid} no encontrado o ya terminado." });
});

// POST /api/process/{pid}/suspend — Suspender proceso
app.MapPost("/api/process/{pid}/suspend", (int pid, ProcessManager pm) =>
{
    bool ok = pm.SuspendProcess(pid);
    return ok
        ? Results.Ok(new { message = $"PID {pid} suspendido." })
        : Results.BadRequest(new { error = $"No se pudo suspender PID {pid}." });
});

// POST /api/process/{pid}/resume — Reanudar proceso
app.MapPost("/api/process/{pid}/resume", (int pid, ProcessManager pm) =>
{
    bool ok = pm.ResumeProcess(pid);
    return ok
        ? Results.Ok(new { message = $"PID {pid} reanudado." })
        : Results.BadRequest(new { error = $"No se pudo reanudar PID {pid}." });
});

// POST /api/tick — Avanzar un ciclo de reloj
app.MapPost("/api/tick", (ProcessManager pm) =>
{
    bool actividad = pm.RunCycle();
    return Results.Ok(new
    {
        ciclo = pm.CicloActual,
        actividad,
        cpuPID = pm.CurrentProcess?.PID,
        procesosActivos = pm.ProcesosActivos
    });
});

// POST /api/demo/producer-consumer — Lanzar demo Productor-Consumidor
app.MapPost("/api/demo/producer-consumer", (ProcessManager pm, IScheduler sched, EventLogger log) =>
{
    var demo = new ProducerConsumerDemo();
    demo.SetupDemo(pm, sched, log, bufferSize: 3, producerBurst: 8, consumerBurst: 8);
    return Results.Ok(new { message = "Demo Productor-Consumidor configurada. Avanza con Tick." });
});

// POST /api/scheduler/change — Cambiar algoritmo de planificación
app.MapPost("/api/scheduler/change", (HttpRequest req, ProcessManager pm, EventLogger log) =>
{
    string algo = req.Query["algo"].ToString().ToLower();
    int quantum = 3; // valor por defecto
    if (req.Query.ContainsKey("quantum"))
        int.TryParse(req.Query["quantum"], out quantum);

    IScheduler newScheduler;
    switch (algo)
    {
        case "fcfs":
            newScheduler = new FCFSScheduler(log);
            break;
        case "rr":
        case "roundrobin":
            if (quantum <= 0) quantum = 3;
            newScheduler = new RoundRobinScheduler(quantum, log);
            break;
        case "sjf":
            newScheduler = new SJFScheduler(log);
            break;
        case "priority":
        case "prioridades":
            newScheduler = new PriorityScheduler(log);
            break;
        default:
            return Results.BadRequest(new { error = $"Algoritmo '{algo}' no reconocido. Use 'fcfs', 'rr', 'sjf' o 'priority'." });
    }

    pm.ChangeScheduler(newScheduler);
    return Results.Ok(new { message = $"Algoritmo cambiado a: {newScheduler.NombreAlgoritmo}" });
});

app.Run();
