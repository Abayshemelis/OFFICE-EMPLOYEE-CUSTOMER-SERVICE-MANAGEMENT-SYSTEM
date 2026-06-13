let currentSessionUser = null;
let activeRequest = null;
let timerInterval = null;
let timerSeconds = 0;

// On load
window.onload = async () => {
    currentSessionUser = API.getCurrentUser();
    if (!currentSessionUser || currentSessionUser.role !== 'Assistant') {
        window.location.href = '../index.html';
        return;
    }

    document.getElementById('user-display').innerText = currentSessionUser.fullName;

    // Check shift checklist
    if (!sessionStorage.getItem('shift_checklist_acknowledged')) {
        openChecklistModal(false);
    }

    // Load initial tab
    switchTab('tasks');

    // Initialize SignalR
    setupSignalR(
        currentSessionUser.userId,
        (notif) => {
            // Toast notification
            Utils.toast(notif.message, notif.title, notif.type === 'SystemAlert' ? 'danger' : 'info');
            loadTasks();
            loadRelayRequests();
        },
        () => {
            loadQueue();
            loadRelayRequests();
        }
    );
};

// Toggle Checkboxes in checklist
function toggleCheckbox(id) {
    const box = document.getElementById(id);
    box.checked = !box.checked;
}

// Open / Close checklist modal
function openChecklistModal(allowClose) {
    const modal = document.getElementById('checklist-modal');
    modal.classList.add('active');
    
    // Disable or enable commencement btn depending on state
    if (sessionStorage.getItem('shift_checklist_acknowledged')) {
        document.querySelectorAll('.checklist-box').forEach(b => b.checked = true);
        document.getElementById('start-shift-btn').innerText = 'Close checklist';
    } else {
        document.getElementById('start-shift-btn').innerText = 'Acknowledge & Commence Shift';
    }
}

function submitChecklist() {
    if (sessionStorage.getItem('shift_checklist_acknowledged')) {
        document.getElementById('checklist-modal').classList.remove('active');
        return;
    }

    const boxes = document.querySelectorAll('.checklist-box');
    let allChecked = true;
    boxes.forEach(b => {
        if (!b.checked) allChecked = false;
    });

    if (!allChecked) {
        Utils.toast('Please review and check all conduct standards before starting your shift.', 'Standards Checklist', 'warning');
        return;
    }

    sessionStorage.setItem('shift_checklist_acknowledged', 'true');
    document.getElementById('checklist-modal').classList.remove('active');
    Utils.toast('Shift started successfully. Have a professional shift!', 'Shift Started', 'success');
}

// Tab Switching
function switchTab(tabId) {
    document.querySelectorAll('.tab-pane').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));

    document.getElementById('tab-' + tabId).style.display = 'block';
    
    // Find active nav item
    const navItems = document.querySelectorAll('.nav-item');
    if (tabId === 'tasks') navItems[0].classList.add('active');
    if (tabId === 'queue') navItems[1].classList.add('active');
    if (tabId === 'relay') navItems[2].classList.add('active');

    // Trigger tab refresh
    if (tabId === 'tasks') loadTasks();
    if (tabId === 'queue') loadQueue();
    if (tabId === 'relay') loadRelayRequests();
}

// Tasks Tab
async function loadTasks() {
    const res = await API.getTasks();
    if (res.success) {
        const tasks = res.data;
        
        const pending = tasks.filter(t => t.status === 'Pending');
        const inProgress = tasks.filter(t => t.status === 'InProgress');
        const completed = tasks.filter(t => t.status === 'Completed' || t.status === 'Cancelled');

        document.getElementById('count-pending').innerText = pending.length;
        document.getElementById('count-inprogress').innerText = inProgress.length;
        document.getElementById('count-completed').innerText = completed.length;

        renderTaskList('list-pending', pending);
        renderTaskList('list-inprogress', inProgress);
        renderTaskList('list-completed', completed);
    }
}

function renderTaskList(containerId, tasks) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';
    
    if (tasks.length === 0) {
        container.innerHTML = '<div style="padding:15px;text-align:center;color:var(--text-secondary);font-size:13px;">No tasks.</div>';
        return;
    }

    tasks.forEach(t => {
        const isOverdue = t.status !== 'Completed' && t.deadline && new Date(t.deadline) < new Date();
        const overdueText = isOverdue ? '<span class="badge bg-danger" style="margin-top:5px;font-size:10px;">Overdue</span>' : '';
        
        let actionBtn = '';
        if (t.status === 'Pending') {
            actionBtn = `<button class="btn btn-primary" style="padding:6px 12px; font-size:12px; margin-top:10px;" onclick="changeTaskStatus(${t.taskId}, 'InProgress')">Start</button>`;
        } else if (t.status === 'InProgress') {
            actionBtn = `
                <div style="display:flex;gap:5px;margin-top:10px;">
                    <button class="btn btn-primary" style="padding:6px 12px; font-size:12px;" onclick="changeTaskStatus(${t.taskId}, 'Completed')">Complete</button>
                    <button class="btn btn-secondary" style="padding:6px 12px; font-size:12px;" onclick="promptTaskHold(${t.taskId})">Hold</button>
                </div>`;
        }

        const deadlineVal = Utils.formatDate(t.deadline);

        const card = document.createElement('div');
        card.className = 'task-card';
        card.innerHTML = `
            <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:8px;">
                <strong style="color:var(--text-primary); font-size:14px;">${t.title}</strong>
                ${Utils.getPriorityBadge(t.priority)}
            </div>
            <p style="font-size:12px; margin-bottom:8px;">${t.description || 'No description'}</p>
            <div style="font-size:11px; color:var(--text-secondary);">Due: ${deadlineVal} ${overdueText}</div>
            <div style="display:flex; justify-content:space-between; align-items:center;">
                ${actionBtn}
                <span style="font-size:11px; cursor:pointer; color:var(--primary-color); margin-top:10px;" onclick="showAuditLog(${t.taskId})">Logs</span>
            </div>
        `;
        container.appendChild(card);
    });
}

async function changeTaskStatus(taskId, status, note = '') {
    const res = await API.updateTaskStatus(taskId, status, note);
    if (res.success) {
        Utils.toast(`Task status updated to ${status}`, 'Task Updated', 'success');
        loadTasks();
    } else {
        Utils.toast(res.message, 'Update Failed', 'danger');
    }
}

function promptTaskHold(taskId) {
    const reason = prompt('Please enter the reason for placing this task On Hold:');
    if (reason === null) return; // cancelled prompt
    changeTaskStatus(taskId, 'InProgress', reason); // Actually updates via note mapping. Let's send OnHold
    API.updateTaskStatus(taskId, 'OnHold', reason).then(() => {
        Utils.toast('Task set to On Hold.', 'Task Update', 'warning');
        loadTasks();
    });
}

async function showAuditLog(taskId) {
    const res = await API.getTaskAudit(taskId);
    if (res.success) {
        const container = document.getElementById('audit-log-container');
        container.innerHTML = '';
        
        if (res.data.length === 0) {
            container.innerHTML = '<p style="text-align:center;color:var(--text-secondary);">No audits logged for this task.</p>';
        } else {
            res.data.forEach(log => {
                const item = document.createElement('div');
                item.style.padding = '12px';
                item.style.borderBottom = '1px solid var(--panel-border)';
                item.innerHTML = `
                    <div style="display:flex;justify-content:space-between;font-size:13px;font-weight:600;">
                        <span>Changed by: ${log.changedByName}</span>
                        <span style="color:var(--text-secondary);font-size:11px;">${Utils.formatDate(log.changedAt)}</span>
                    </div>
                    <div style="font-size:12px;margin-top:4px;">
                        Status: <span style="text-decoration:line-through;color:var(--text-secondary);">${log.oldStatus || 'None'}</span> &rarr; <strong style="color:var(--primary-color);">${log.newStatus}</strong>
                    </div>
                    <p style="font-size:12px;font-style:italic;margin-top:4px;color:var(--text-secondary);">${log.changeNote || ''}</p>
                `;
                container.appendChild(item);
            });
        }
        document.getElementById('audit-modal').classList.add('active');
    }
}

function closeAuditModal() {
    document.getElementById('audit-modal').classList.remove('active');
}

// Queue Tab
async function loadQueue() {
    const res = await API.getQueue();
    if (res.success) {
        const activeQueue = res.data;
        const tbody = document.getElementById('queue-list-body');
        tbody.innerHTML = '';

        const waiting = activeQueue.filter(q => q.status === 'Waiting');
        const serving = activeQueue.find(q => q.status === 'InService' && q.assistantId === currentSessionUser.userId);

        if (serving) {
            // Restore active service display
            activeRequest = serving;
            document.getElementById('active-service-panel').style.display = 'block';
            document.getElementById('current-cust-name').innerText = serving.customerName;
            document.getElementById('current-cust-desc').innerText = 'Service Need: ' + serving.serviceDescription;
            startServiceTimer(serving.serviceStartTime);
        } else {
            stopServiceTimer();
            activeRequest = null;
            document.getElementById('active-service-panel').style.display = 'none';
        }

        if (waiting.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="padding:20px; text-align:center;" class="p">Lobby is currently empty.</td></tr>';
            return;
        }

        waiting.forEach(item => {
            const tr = document.createElement('tr');
            tr.style.borderBottom = '1px solid var(--panel-border)';
            tr.innerHTML = `
                <td style="padding: 12px; font-weight:600; color:var(--primary-color);">#${String(item.queueNumber).padStart(2, '0')}</td>
                <td style="padding: 12px; font-weight:500;">${item.customerName}</td>
                <td style="padding: 12px; font-size:13px;">${item.serviceDescription}</td>
                <td style="padding: 12px; font-size:13px; color:var(--text-secondary);">${Utils.formatDate(item.createdAt)}</td>
                <td style="padding: 12px;">${Utils.getStatusBadge(item.status)}</td>
            `;
            tbody.appendChild(tr);
        });
    }
}

async function callNextCustomer() {
    if (activeRequest) {
        Utils.toast('You are currently serving a customer. Resolve the active request first.', 'Queue Manager', 'warning');
        return;
    }

    const res = await API.getQueue();
    if (res.success) {
        const waiting = res.data.filter(q => q.status === 'Waiting');
        if (waiting.length === 0) {
            Utils.toast('No customers waiting in the lobby.', 'Queue Manager', 'info');
            return;
        }

        const next = waiting[0];
        // Stand and greet client prompt
        Utils.toast(`Stand & Greet prompt: "Stand, smile, and greet ${next.customerName} politely."`, 'Service Greeting Protocol', 'info');

        const updateRes = await API.updateRequestStatus(next.requestId, 'InService');
        if (updateRes.success) {
            Utils.toast(`Called Ticket #${next.queueNumber} (${next.customerName})`, 'Queue Action', 'success');
            loadQueue();
        }
    }
}

function startServiceTimer(startTimeStr) {
    if (timerInterval) clearInterval(timerInterval);
    
    const startTime = startTimeStr ? new Date(startTimeStr) : new Date();
    
    timerInterval = setInterval(() => {
        const now = new Date();
        const diff = Math.floor((now - startTime) / 1000);
        timerSeconds = diff >= 0 ? diff : 0;
        
        const mm = String(Math.floor(timerSeconds / 60)).padStart(2, '0');
        const ss = String(timerSeconds % 60).padStart(2, '0');
        document.getElementById('service-timer').innerText = `${mm}:${ss}`;
    }, 1000);
}

function stopServiceTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
    document.getElementById('service-timer').innerText = '00:00';
}

async function resolveService(status) {
    const summary = document.getElementById('cust-needs-summary').value;
    const notes = document.getElementById('resolution-note').value;

    if (!summary.trim()) {
        Utils.toast('Active Listening: You must enter a "Customer Needs Summary" before resolving the visit.', 'Verification Error', 'warning');
        return;
    }

    const res = await API.updateRequestStatus(activeRequest.requestId, status, `Needs Summary: ${summary}. Actions: ${notes}`);
    if (res.success) {
        Utils.toast(`Visit resolved as: ${status}`, 'Service Closed', 'success');
        
        // Reset active fields
        document.getElementById('cust-needs-summary').value = '';
        document.getElementById('resolution-note').value = '';
        
        stopServiceTimer();
        loadQueue();
    } else {
        Utils.toast(res.message, 'Failed to resolve request', 'danger');
    }
}

// Relay Tab
async function loadRelayRequests() {
    const res = await API.getContactRequests();
    if (res.success) {
        const list = res.data;
        const tbody = document.getElementById('relay-list-body');
        tbody.innerHTML = '';

        // Update badge unread relay
        const unreadRelays = list.filter(r => r.status === 'Pending' || r.status === 'Replied');
        const badge = document.getElementById('relay-count');
        if (unreadRelays.length > 0) {
            badge.innerText = unreadRelays.length;
            badge.style.display = 'inline-flex';
        } else {
            badge.style.display = 'none';
        }

        if (list.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="padding:20px; text-align:center;" class="p">No escalation requests logged.</td></tr>';
            return;
        }

        list.forEach(item => {
            let actionBtn = '';
            if (item.status === 'Pending') {
                actionBtn = `<button class="btn btn-primary btn-sm" style="padding:5px 10px; font-size:12px;" onclick="openForwardModal(${item.contactRequestId})">Forward to Manager</button>`;
            } else if (item.status === 'Replied') {
                actionBtn = `<button class="btn btn-success btn-sm" style="padding:5px 10px; font-size:12px;" onclick="closeRelay(${item.contactRequestId})">Close & Relay to Customer</button>`;
            } else {
                actionBtn = `<span style="font-size:12px; color:var(--text-secondary);">No action</span>`;
            }

            const tr = document.createElement('tr');
            tr.style.borderBottom = '1px solid var(--panel-border)';
            tr.innerHTML = `
                <td style="padding: 12px; font-weight:600;">${item.customerName}</td>
                <td style="padding: 12px; font-size:13px;">"${item.customerMessage}"</td>
                <td style="padding: 12px; font-size:13px; color:#4ade80;">${item.managerReply ? `"${item.managerReply}"` : '<span style="color:var(--text-secondary);font-style:italic;">Awaiting Manager Reply</span>'}</td>
                <td style="padding: 12px;">${Utils.getStatusBadge(item.status)}</td>
                <td style="padding: 12px; text-align:right;">${actionBtn}</td>
            `;
            tbody.appendChild(tr);
        });
    }
}

function openForwardModal(id) {
    document.getElementById('forward-contact-id').value = id;
    document.getElementById('asst-forward-note').value = '';
    document.getElementById('forward-modal').classList.add('active');
}

function closeForwardModal() {
    document.getElementById('forward-modal').classList.remove('active');
}

document.getElementById('forward-form').onsubmit = async (e) => {
    e.preventDefault();
    const id = Number(document.getElementById('forward-contact-id').value);
    const note = document.getElementById('asst-forward-note').value;

    const res = await API.forwardContactRequest(id, note);
    if (res.success) {
        Utils.toast('Message forwarded to Manager inbox.', 'Escalated', 'success');
        closeForwardModal();
        loadRelayRequests();
    } else {
        Utils.toast(res.message, 'Forwarding Failed', 'danger');
    }
};

async function closeRelay(id) {
    const res = await API.closeContactRequest(id);
    if (res.success) {
        Utils.toast('Request Closed. Make sure you deliver the response verbally or print it.', 'Request Closed', 'success');
        loadRelayRequests();
    }
}

function logout() {
    API.logout();
    window.location.href = '../index.html';
}
