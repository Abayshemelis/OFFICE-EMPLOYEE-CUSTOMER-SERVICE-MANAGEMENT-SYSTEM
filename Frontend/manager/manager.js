let currentSessionUser = null;

// On load
window.onload = async () => {
    currentSessionUser = API.getCurrentUser();
    if (!currentSessionUser || currentSessionUser.role !== 'Manager') {
        window.location.href = '../index.html';
        return;
    }

    document.getElementById('header-title').innerText = `Welcome, ${currentSessionUser.fullName}`;

    // Load initial data
    switchTab('tasks');

    // Initialize SignalR
    setupSignalR(
        currentSessionUser.userId,
        (notif) => {
            Utils.toast(notif.message, notif.title, notif.type === 'SystemAlert' ? 'danger' : 'info');
            // Reload relevant data
            loadTasks();
            loadInbox();
        },
        () => {
            // Queue update — refresh logs
            loadServiceLogs();
        }
    );
};

// ================== TAB SWITCHING ==================
function switchTab(tabId) {
    document.querySelectorAll('.tab-pane').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));

    document.getElementById('tab-' + tabId).style.display = 'block';

    const navItems = document.querySelectorAll('.nav-item');
    const tabMap = { tasks: 0, assistants: 1, inbox: 2, reports: 3, logs: 4 };
    if (tabMap[tabId] !== undefined) navItems[tabMap[tabId]].classList.add('active');

    if (tabId === 'tasks') loadTasks();
    if (tabId === 'assistants') loadAssistants();
    if (tabId === 'inbox') loadInbox();
    if (tabId === 'reports') loadReports();
    if (tabId === 'logs') loadServiceLogs();
}

// ================== TASKS TAB ==================
async function loadTasks() {
    const res = await API.getTasks();
    if (!res.success) return;

    const tasks = res.data;
    const tbody = document.getElementById('task-list-body');
    tbody.innerHTML = '';

    if (tasks.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="padding:20px;text-align:center;" class="p">No tasks registered.</td></tr>';
        return;
    }

    tasks.forEach(t => {
        const isOverdue = t.status !== 'Completed' && t.status !== 'Cancelled' && t.deadline && new Date(t.deadline) < new Date();

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight:600;">${t.title} ${isOverdue ? '<span class="badge bg-danger" style="margin-left:8px;font-size:10px;">OVERDUE</span>' : ''}</td>
            <td>${t.assignedToName || 'Unassigned'}</td>
            <td>${Utils.getPriorityBadge(t.priority)}</td>
            <td style="font-size:13px; color:var(--text-secondary);">${Utils.formatDate(t.deadline)}</td>
            <td>${Utils.getStatusBadge(t.status)}</td>
            <td>
                <div class="actions-cell">
                    ${t.status !== 'Completed' && t.status !== 'Cancelled' ? `
                        <button class="btn btn-secondary" style="padding:5px 10px;font-size:12px;" onclick="openReassignPrompt(${t.taskId})">Reassign</button>
                        <button class="btn btn-danger" style="padding:5px 10px;font-size:12px;" onclick="cancelTask(${t.taskId})">Cancel</button>
                    ` : ''}
                    <button class="btn btn-secondary" style="padding:5px 10px;font-size:12px;" onclick="showAuditLog(${t.taskId})">Logs</button>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function openCreateTaskModal() {
    document.getElementById('task-modal-title').innerText = 'Create New Task';
    document.getElementById('task-form').reset();
    document.getElementById('task-id').value = '';
    loadAssistantsDropdown();
    document.getElementById('task-modal').classList.add('active');
}

function closeTaskModal() {
    document.getElementById('task-modal').classList.remove('active');
}

async function loadAssistantsDropdown() {
    const res = await API.getUsers();
    if (!res.success) return;

    const select = document.getElementById('task-assignee');
    select.innerHTML = '';

    const assistants = res.data.filter(u => u.role === 'Assistant' && u.isActive !== false);
    assistants.forEach(a => {
        const option = document.createElement('option');
        option.value = a.userId;
        option.textContent = a.fullName;
        select.appendChild(option);
    });

    if (assistants.length === 0) {
        const option = document.createElement('option');
        option.textContent = 'No assistants available';
        option.disabled = true;
        select.appendChild(option);
    }
}

document.getElementById('task-form').onsubmit = async (e) => {
    e.preventDefault();
    const title = document.getElementById('task-title').value;
    const desc = document.getElementById('task-desc').value;
    const priority = document.getElementById('task-priority').value;
    const assignee = document.getElementById('task-assignee').value;
    const deadline = document.getElementById('task-deadline').value;
    const notes = document.getElementById('task-notes').value;

    const res = await API.createTask(title, desc, priority, assignee, deadline, notes);
    if (res.success) {
        Utils.toast('Task created and assigned to the selected assistant.', 'Task Created', 'success');
        closeTaskModal();
        loadTasks();
    } else {
        Utils.toast(res.message, 'Error Creating Task', 'danger');
    }
};

async function openReassignPrompt(taskId) {
    const res = await API.getUsers();
    if (!res.success) return;

    const assistants = res.data.filter(u => u.role === 'Assistant' && u.isActive !== false);
    if (assistants.length === 0) {
        Utils.toast('No active assistants available for reassignment.', 'Reassign Failed', 'warning');
        return;
    }

    const nameList = assistants.map((a, i) => `${i + 1}. ${a.fullName} (ID: ${a.userId})`).join('\n');
    const input = prompt(`Select new assignee by ID:\n\n${nameList}`);
    if (input === null) return;

    const newId = parseInt(input);
    if (isNaN(newId)) {
        Utils.toast('Invalid assistant ID entered.', 'Reassignment Error', 'danger');
        return;
    }

    const reassignRes = await API.reassignTask(taskId, newId);
    if (reassignRes.success) {
        Utils.toast('Task reassigned successfully.', 'Task Reassigned', 'success');
        loadTasks();
    } else {
        Utils.toast(reassignRes.message || 'Failed to reassign.', 'Error', 'danger');
    }
}

async function cancelTask(taskId) {
    if (!confirm('Are you sure you want to cancel this task?')) return;
    const res = await API.deleteTask(taskId);
    if (res.success) {
        Utils.toast('Task cancelled successfully.', 'Task Cancelled', 'success');
        loadTasks();
    }
}

async function showAuditLog(taskId) {
    const res = await API.getTaskAudit(taskId);
    if (!res.success) return;

    const container = document.getElementById('audit-log-container');
    container.innerHTML = '';

    if (res.data.length === 0) {
        container.innerHTML = '<p style="text-align:center;color:var(--text-secondary);">No audits logged.</p>';
    } else {
        res.data.forEach(log => {
            const item = document.createElement('div');
            item.style.padding = '12px';
            item.style.borderBottom = '1px solid var(--panel-border)';
            item.innerHTML = `
                <div style="display:flex;justify-content:space-between;font-size:13px;font-weight:600;">
                    <span>By: ${log.changedByName}</span>
                    <span style="color:var(--text-secondary);font-size:11px;">${Utils.formatDate(log.changedAt)}</span>
                </div>
                <div style="font-size:12px;margin-top:4px;">
                    <span style="text-decoration:line-through;color:var(--text-secondary);">${log.oldStatus || 'None'}</span> &rarr; <strong style="color:var(--primary-color);">${log.newStatus}</strong>
                </div>
                <p style="font-size:12px;font-style:italic;margin-top:4px;color:var(--text-secondary);">${log.changeNote || ''}</p>
            `;
            container.appendChild(item);
        });
    }
    document.getElementById('audit-modal').classList.add('active');
}

function closeAuditModal() {
    document.getElementById('audit-modal').classList.remove('active');
}

// ================== ASSISTANTS TAB ==================
async function loadAssistants() {
    const res = await API.getUsers();
    if (!res.success) return;

    const assistants = res.data.filter(u => u.role === 'Assistant');
    const tbody = document.getElementById('assistant-list-body');
    tbody.innerHTML = '';

    if (assistants.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="padding:20px;text-align:center;" class="p">No assistants registered.</td></tr>';
        return;
    }

    assistants.forEach(a => {
        const statusBadge = a.isActive !== false
            ? '<span class="badge bg-success">Active</span>'
            : '<span class="badge bg-danger">Deactivated</span>';

        const toggleBtn = a.isActive !== false
            ? `<button class="btn btn-danger" style="padding:5px 10px;font-size:12px;" onclick="toggleStatus(${a.userId}, false)">Deactivate</button>`
            : `<button class="btn btn-primary" style="padding:5px 10px;font-size:12px;" onclick="toggleStatus(${a.userId}, true)">Activate</button>`;

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight:600;">${a.fullName}</td>
            <td>${a.username}</td>
            <td style="font-size:13px;">${a.email || 'N/A'}</td>
            <td style="font-size:13px;">${a.phone || 'N/A'}</td>
            <td>${statusBadge}</td>
            <td>
                <div class="actions-cell">
                    ${toggleBtn}
                    <button class="btn btn-secondary" style="padding:5px 10px;font-size:12px;" onclick="viewPerformance(${a.userId})">Performance</button>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function openCreateAssistantModal() {
    document.getElementById('assistant-form').reset();
    document.getElementById('assistant-modal').classList.add('active');
}

function closeAssistantModal() {
    document.getElementById('assistant-modal').classList.remove('active');
}

document.getElementById('assistant-form').onsubmit = async (e) => {
    e.preventDefault();
    const fullName = document.getElementById('asst-fullname').value;
    const username = document.getElementById('asst-username').value;
    const password = document.getElementById('asst-password').value;
    const email = document.getElementById('asst-email').value;
    const phone = document.getElementById('asst-phone').value;

    // Basic validation
    if (password.length < 8) {
        Utils.toast('Password must be at least 8 characters.', 'Validation Error', 'warning');
        return;
    }

    const res = await API.createAssistant(fullName, username, password, email, phone);
    if (res.success) {
        Utils.toast(`Account for ${fullName} created successfully.`, 'Account Created', 'success');
        closeAssistantModal();
        loadAssistants();
    } else {
        Utils.toast(res.message, 'Account Creation Failed', 'danger');
    }
};

async function toggleStatus(userId, isActive) {
    const action = isActive ? 'activate' : 'deactivate';
    if (!confirm(`Are you sure you want to ${action} this assistant?`)) return;

    const res = await API.toggleUserStatus(userId, isActive);
    if (res.success) {
        Utils.toast(`User has been ${action}d successfully.`, 'Account Updated', 'success');
        loadAssistants();
    }
}

async function viewPerformance(userId) {
    switchTab('reports');
    // Highlight the specific assistant
    setTimeout(async () => {
        const res = await API.getPerformance(userId);
        if (res.success) {
            const d = res.data;
            Utils.toast(`Performance for ${d.fullName}: Tasks ${d.tasksCompleted}/${d.totalTasksAssigned}, Rating ${d.averageRating}★`, 'Quick Report', 'info');
        }
    }, 300);
}

// ================== INBOX / RELAY TAB ==================
async function loadInbox() {
    const res = await API.getContactRequests();
    if (!res.success) return;

    const list = res.data;
    const tbody = document.getElementById('inbox-list-body');
    tbody.innerHTML = '';

    // Update badge
    const forwarded = list.filter(r => r.status === 'Forwarded');
    const badge = document.getElementById('inbox-count');
    if (forwarded.length > 0) {
        badge.innerText = forwarded.length;
        badge.style.display = 'inline-flex';
    } else {
        badge.style.display = 'none';
    }

    if (list.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="padding:20px;text-align:center;" class="p">No messages in inbox.</td></tr>';
        return;
    }

    list.forEach(item => {
        let actionBtn = '';
        if (item.status === 'Forwarded') {
            actionBtn = `<button class="btn btn-primary" style="padding:5px 10px;font-size:12px;" onclick="openReplyModal(${item.contactRequestId}, \`${escapeHTML(item.customerMessage)}\`)">Reply</button>`;
        } else if (item.status === 'Replied') {
            actionBtn = `<span class="badge bg-success">Reply Sent</span>`;
        } else if (item.status === 'Closed') {
            actionBtn = `<span class="badge bg-info">Closed</span>`;
        } else {
            actionBtn = `<span class="badge bg-warning">Pending Assistant</span>`;
        }

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight:600;">${item.customerName}</td>
            <td style="font-size:13px; color:var(--text-secondary);">${item.assistantNote || '<i>No note</i>'}</td>
            <td style="font-size:13px;">${item.customerComment || ''}</td>
            <td>${Utils.getStatusBadge(item.status)}</td>
            <td><div class="actions-cell">${actionBtn}</div></td>
        `;
        tbody.appendChild(tr);
    });
}

function escapeHTML(str) {
    if (!str) return '';
    return str.replace(/`/g, '\\`').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function openReplyModal(contactId, customerMsg) {
    document.getElementById('reply-contact-id').value = contactId;
    document.getElementById('reply-cust-msg').innerText = customerMsg;
    document.getElementById('manager-reply-text').value = '';
    document.getElementById('reply-modal').classList.add('active');
}

function closeReplyModal() {
    document.getElementById('reply-modal').classList.remove('active');
}

document.getElementById('reply-form').onsubmit = async (e) => {
    e.preventDefault();
    const id = Number(document.getElementById('reply-contact-id').value);
    const msg = document.getElementById('manager-reply-text').value;

    const res = await API.replyContactRequest(id, msg);
    if (res.success) {
        Utils.toast('Reply posted. The assistant will relay this to the customer.', 'Reply Sent', 'success');
        closeReplyModal();
        loadInbox();
    } else {
        Utils.toast(res.message, 'Reply Error', 'danger');
    }
};

// ================== REPORTS TAB ==================
async function loadReports() {
    const usersRes = await API.getUsers();
    if (!usersRes.success) return;

    const assistants = usersRes.data.filter(u => u.role === 'Assistant');
    const tbody = document.getElementById('reports-list-body');
    tbody.innerHTML = '';

    // Aggregate metrics
    let totalAllTasks = 0, totalCompleted = 0, totalOverdue = 0;
    let totalRatingSum = 0, ratingCount = 0;

    for (const asst of assistants) {
        const perfRes = await API.getPerformance(asst.userId);
        if (!perfRes.success) continue;

        const d = perfRes.data;
        totalAllTasks += d.totalTasksAssigned;
        totalCompleted += d.tasksCompleted;
        totalOverdue += d.tasksOverdue;
        if (d.averageRating > 0) {
            totalRatingSum += d.averageRating;
            ratingCount++;
        }

        const activeTasks = d.totalTasksAssigned - d.tasksCompleted - d.tasksOverdue;
        const ratingColor = d.averageRating < 3 ? 'color:var(--danger-color);font-weight:700;' : 'color:var(--warning-color);';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight:600;">${d.fullName}</td>
            <td>${activeTasks >= 0 ? activeTasks : 0}</td>
            <td style="color:var(--success-color);font-weight:600;">${d.tasksCompleted}</td>
            <td style="color:var(--danger-color);font-weight:600;">${d.tasksOverdue}</td>
            <td style="${ratingColor}">${d.averageRating > 0 ? d.averageRating + ' ★' : 'No ratings'}</td>
            <td>${d.totalEscalations}</td>
        `;
        tbody.appendChild(tr);
    }

    if (assistants.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="padding:20px;text-align:center;" class="p">No assistants to report on.</td></tr>';
    }

    // Update summary metrics
    document.getElementById('rep-total-tasks').innerText = totalAllTasks;
    document.getElementById('rep-completed-tasks').innerText = totalCompleted;
    document.getElementById('rep-overdue-tasks').innerText = totalOverdue;
    document.getElementById('rep-avg-rating').innerText = ratingCount > 0 ? (totalRatingSum / ratingCount).toFixed(1) + ' ★' : 'N/A';
}

// ================== SERVICE LOGS TAB ==================
async function loadServiceLogs() {
    const res = await API.getServiceRequests();
    if (!res.success) return;

    const list = res.data;
    const tbody = document.getElementById('logs-list-body');
    tbody.innerHTML = '';

    if (list.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="padding:20px;text-align:center;" class="p">No logs registered.</td></tr>';
        return;
    }

    list.forEach((item, index) => {
        const ratingStars = item.customerRating
            ? '★'.repeat(item.customerRating) + '☆'.repeat(5 - item.customerRating)
            : '<span style="color:var(--text-secondary);">Not rated</span>';
        const ratingColor = item.customerRating && item.customerRating < 3 ? 'color:var(--danger-color);' : 'color:var(--warning-color);';

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td style="font-weight:600; color:var(--primary-color);">#${String(item.queueNumber || index + 1).padStart(2, '0')}</td>
            <td style="font-weight:500;">${item.customerName}</td>
            <td style="font-size:13px;">${item.serviceDescription}</td>
            <td>${item.assistantName}</td>
            <td style="${ratingColor}">${ratingStars}</td>
            <td style="font-size:13px; font-style:italic;">${item.customerFeedback ? `"${item.customerFeedback}"` : '<span style="color:var(--text-secondary);">None</span>'}</td>
            <td style="font-size:13px; color:var(--text-secondary);">${Utils.formatDate(item.createdAt)}</td>
        `;
        tbody.appendChild(tr);
    });
}

// ================== LOGOUT ==================
function logout() {
    API.logout();
    window.location.href = '../index.html';
}
