// ═══════════════════════════════════════════════════════════════
// app.js — Frontend del Simulador de Gestor de Procesos
// Polling cada segundo + control manual vía botones
// ═══════════════════════════════════════════════════════════════

let autoTickInterval = null;
let currentFilter = 'all';
let lastLogCount = 0;

// ── Inicialización ─────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    fetchStatus();
    setInterval(fetchStatus, 1000);
});

// ── Polling: GET /api/status ───────────────────────────────────
async function fetchStatus() {
    try {
        const res = await fetch('/api/status');
        if (!res.ok) return;
        const data = await res.json();
        updateDashboard(data);
    } catch (err) {
        console.error('Error fetching status:', err);
    }
}

// ── Actualizar todo el dashboard ───────────────────────────────
function updateDashboard(data) {
    // Header
    document.getElementById('cycle-count').textContent = data.ciclo;
    document.getElementById('active-count').textContent = data.procesosActivos;

    // CPU
    const cpuEl = document.getElementById('cpu-status');
    if (data.cpu) {
        cpuEl.className = 'cpu-status running';
        cpuEl.textContent = `PID ${String(data.cpu.pid).padStart(3, '0')} (${data.cpu.remainingTime}/${data.cpu.burstTime}ms)`;
    } else {
        cpuEl.className = 'cpu-status idle';
        cpuEl.textContent = 'Ociosa';
    }

    // RAM
    const ram = data.ram;
    const ramBar = document.getElementById('ram-bar');
    const ramText = document.getElementById('ram-text');
    ramBar.style.width = ram.ramUsagePercent + '%';
    ramBar.className = 'ram-bar' + (ram.ramUsagePercent > 80 ? ' high' : '');
    ramText.textContent = `${ram.usedRAM} / ${ram.total} MB (${ram.ramUsagePercent}%)`;

    // Scheduler
    document.getElementById('scheduler-algo').textContent = data.scheduler.algoritmo;
    document.getElementById('scheduler-queue').textContent = `En cola: ${data.scheduler.enCola}`;

    // Procesos
    updateProcessTable(data.procesos);

    // Logs
    updateLogs(data.logs);
}

// ── Tabla de Procesos ──────────────────────────────────────────
function updateProcessTable(procesos) {
    const tbody = document.getElementById('process-tbody');

    let filtered = procesos;
    if (currentFilter === 'ready') filtered = procesos.filter(p => p.estado === 'Listo');
    else if (currentFilter === 'waiting') filtered = procesos.filter(p => p.estado === 'Esperando');
    else if (currentFilter === 'terminated') filtered = procesos.filter(p => p.estado === 'Terminado');

    if (filtered.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="7">Sin procesos en esta categoría</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map(p => {
        const progress = p.burstTime > 0
            ? Math.round(((p.burstTime - p.remainingTime) / p.burstTime) * 100)
            : 100;

        const actions = [];
        if (p.estado === 'Ejecutando' || p.estado === 'Listo') {
            actions.push(`<button class="btn-danger-sm" onclick="killProcess(${p.pid})">Kill</button>`);
        }
        if (p.estado === 'Ejecutando') {
            actions.push(`<button class="btn-info-sm" onclick="suspendProcess(${p.pid})">Suspend</button>`);
        }
        if (p.estado === 'Esperando') {
            actions.push(`<button class="btn-info-sm" onclick="resumeProcess(${p.pid})">Resume</button>`);
            actions.push(`<button class="btn-danger-sm" onclick="killProcess(${p.pid})">Kill</button>`);
        }

        return `<tr>
            <td>${String(p.pid).padStart(3, '0')}</td>
            <td><span class="state-badge state-${p.estado}">${p.estado}</span></td>
            <td>${p.prioridad}</td>
            <td>${p.memoriaRequeridaMB} MB</td>
            <td>
                <div class="progress-bar-cell">
                    <div class="progress-bar-mini">
                        <div class="progress-bar-fill" style="width: ${progress}%"></div>
                    </div>
                    <span class="progress-text">${p.remainingTime}/${p.burstTime}</span>
                </div>
            </td>
            <td>${p.exitReason || '—'}</td>
            <td><div class="actions-cell">${actions.join('')}</div></td>
        </tr>`;
    }).join('');
}

// ── Logs ───────────────────────────────────────────────────────
function updateLogs(logs) {
    const container = document.getElementById('log-container');
    const isAtBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 50;

    if (logs.length !== lastLogCount) {
        container.innerHTML = logs.map(l =>
            `<div class="log-entry">
                <span class="log-time">${l.hora}</span>
                <span class="log-category cat-${l.category}">${l.category}</span>
                <span class="log-msg">${escapeHtml(l.message)}</span>
            </div>`
        ).join('');

        lastLogCount = logs.length;
        if (isAtBottom) container.scrollTop = container.scrollHeight;
    }
}

function clearLogs() {
    document.getElementById('log-container').innerHTML = '';
    lastLogCount = 0;
}

// ── Tabs ───────────────────────────────────────────────────────
function switchTab(tab) {
    currentFilter = tab;
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelector(`.tab[data-tab="${tab}"]`).classList.add('active');
    fetchStatus();
}

// ── Acciones: Tick ─────────────────────────────────────────────
async function doTick() {
    const btn = document.getElementById('btn-tick');
    btn.style.transform = 'scale(0.95)';
    setTimeout(() => btn.style.transform = '', 150);

    try {
        await fetch('/api/tick', { method: 'POST' });
        await fetchStatus();
    } catch (err) {
        console.error('Tick error:', err);
    }
}

function toggleAutoTick() {
    const btn = document.getElementById('btn-auto');
    if (autoTickInterval) {
        clearInterval(autoTickInterval);
        autoTickInterval = null;
        btn.textContent = '⏩ Auto-Tick: OFF';
        btn.classList.remove('active');
    } else {
        autoTickInterval = setInterval(doTick, 500);
        btn.textContent = '⏸ Auto-Tick: ON';
        btn.classList.add('active');
    }
}

// ── Acciones: Crear Proceso ────────────────────────────────────
async function createProcess() {
    const prioridad = document.getElementById('inp-prioridad').value;
    const memoria = document.getElementById('inp-memoria').value;
    const burst = document.getElementById('inp-burst').value;

    try {
        const res = await fetch(
            `/api/process/create?prioridad=${prioridad}&memoria=${memoria}&burst=${burst}`,
            { method: 'POST' }
        );
        const data = await res.json();
        if (!res.ok) alert(data.error);
        await fetchStatus();
    } catch (err) {
        console.error('Create error:', err);
    }
}

// ── Acciones: Kill / Suspend / Resume ──────────────────────────
async function killProcess(pid) {
    try {
        await fetch(`/api/process/${pid}/kill`, { method: 'POST' });
        await fetchStatus();
    } catch (err) { console.error(err); }
}

async function suspendProcess(pid) {
    try {
        await fetch(`/api/process/${pid}/suspend`, { method: 'POST' });
        await fetchStatus();
    } catch (err) { console.error(err); }
}

async function resumeProcess(pid) {
    try {
        await fetch(`/api/process/${pid}/resume`, { method: 'POST' });
        await fetchStatus();
    } catch (err) { console.error(err); }
}

// ── Acciones: Cambiar Scheduler ────────────────────────────────
function toggleQuantumField() {
    const algo = document.getElementById('sel-algo').value;
    const quantumField = document.getElementById('quantum-field');
    quantumField.style.display = algo === 'rr' ? '' : 'none';
}

async function changeScheduler() {
    const algo = document.getElementById('sel-algo').value;
    const quantum = document.getElementById('inp-quantum').value;

    let url = `/api/scheduler/change?algo=${algo}`;
    if (algo === 'rr') url += `&quantum=${quantum}`;

    try {
        const res = await fetch(url, { method: 'POST' });
        const data = await res.json();
        if (!res.ok) {
            alert(data.error);
        }
        await fetchStatus();
    } catch (err) {
        console.error('Change scheduler error:', err);
    }
}

// ── Acciones: Demo Productor-Consumidor ────────────────────────
async function launchProducerConsumer() {
    try {
        await fetch('/api/demo/producer-consumer', { method: 'POST' });
        await fetchStatus();
    } catch (err) { console.error(err); }
}

// ── Utilidades ─────────────────────────────────────────────────
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
