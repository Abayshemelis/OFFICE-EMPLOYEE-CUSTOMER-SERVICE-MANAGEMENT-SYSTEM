// Frontend API Client wrapper with auto-detecting mock mode fallback.
const BACKEND_URL = 'http://localhost:5000';

// Check if we are running in mock mode (fallback when backend is unreachable)
let isMockMode = false;

// Initialize mock storage in localStorage if empty
function initializeMockStorage() {
    if (!localStorage.getItem('oecsms_users')) {
        const users = [
            { userId: 1, username: 'manager', passwordHash: 'Manager123!', fullName: 'System Manager', role: 'Manager', email: 'manager@oecsms.com', phone: '555-0100', isActive: true },
            { userId: 2, username: 'assistant', passwordHash: 'Assistant123!', fullName: 'Alice Assistant', role: 'Assistant', email: 'alice@oecsms.com', phone: '555-0200', isActive: true, managerId: 1 }
        ];
        localStorage.setItem('oecsms_users', JSON.stringify(users));
    }

    if (!localStorage.getItem('oecsms_tasks')) {
        const tasks = [
            { taskId: 1, title: 'Verify Audit Reports', description: 'Review the compliance logs from the last fiscal quarter.', priority: 'High', status: 'Pending', assignedToId: 2, assignedToName: 'Alice Assistant', assignedById: 1, assignedByName: 'System Manager', deadline: '2026-06-12T17:00:00', createdAt: new Date().toISOString(), notes: 'Focus on visitor registration gaps.' },
            { taskId: 2, title: 'Prepare Reception Station', description: 'Ensure the customer kiosk has active connection and printer paper.', priority: 'Medium', status: 'InProgress', assignedToId: 2, assignedToName: 'Alice Assistant', assignedById: 1, assignedByName: 'System Manager', deadline: '2026-06-11T12:00:00', createdAt: new Date().toISOString(), notes: 'Double check visual check list.' }
        ];
        localStorage.setItem('oecsms_tasks', JSON.stringify(tasks));
    }

    if (!localStorage.getItem('oecsms_task_logs')) {
        localStorage.setItem('oecsms_task_logs', JSON.stringify([]));
    }

    if (!localStorage.getItem('oecsms_customers')) {
        const customers = [
            { customerId: 101, fullName: 'John Doe', phone: '555-9001', email: 'john@example.com', visitDate: new Date().toISOString().split('T')[0], arrivalTime: new Date().toISOString(), queueNumber: 1 }
        ];
        localStorage.setItem('oecsms_customers', JSON.stringify(customers));
    }

    if (!localStorage.getItem('oecsms_requests')) {
        const requests = [
            { requestId: 501, customerId: 101, customerName: 'John Doe', queueNumber: 1, assistantId: 2, assistantName: 'Alice Assistant', serviceDescription: 'Requesting account extension assistance.', status: 'Waiting', createdAt: new Date().toISOString() }
        ];
        localStorage.setItem('oecsms_requests', JSON.stringify(requests));
    }

    if (!localStorage.getItem('oecsms_contact_requests')) {
        localStorage.setItem('oecsms_contact_requests', JSON.stringify([]));
    }

    if (!localStorage.getItem('oecsms_notifications')) {
        const notifications = [
            { notificationId: 1, recipientUserId: 2, title: 'Shift Started', message: 'Remember to complete the daily conduct standards checklist.', type: 'SystemAlert', isRead: false, createdAt: new Date().toISOString() }
        ];
        localStorage.setItem('oecsms_notifications', JSON.stringify(notifications));
    }

    if (!localStorage.getItem('oecsms_conduct_scores')) {
        localStorage.setItem('oecsms_conduct_scores', JSON.stringify([]));
    }
}

// Perform simple healthcheck to see if backend is up
async function checkBackendStatus() {
    try {
        const response = await fetch(`${BACKEND_URL}/health`, { method: 'GET' });
        if (response.ok) {
            isMockMode = false;
            console.log("Connected to C# Backend API.");
        } else {
            throw new Error('Health check failed');
        }
    } catch (e) {
        isMockMode = true;
        initializeMockStorage();
        console.warn("Backend API unreachable. Falling back to Client Mock Storage (Local).");
        showModeIndicator();
    }
}

function showModeIndicator() {
    // Add a floating badge indicating mock mode
    let badge = document.getElementById('mock-mode-badge');
    if (!badge) {
        badge = document.createElement('div');
        badge.id = 'mock-mode-badge';
        badge.style.position = 'fixed';
        badge.style.bottom = '10px';
        badge.style.right = '10px';
        badge.style.background = 'rgba(255, 152, 0, 0.9)';
        badge.style.color = '#fff';
        badge.style.padding = '6px 12px';
        badge.style.borderRadius = '20px';
        badge.style.fontFamily = 'system-ui, sans-serif';
        badge.style.fontSize = '12px';
        badge.style.fontWeight = 'bold';
        badge.style.boxShadow = '0 2px 8px rgba(0,0,0,0.2)';
        badge.style.zIndex = '9999';
        badge.innerText = 'Demo Mode (Offline Fallback)';
        document.body.appendChild(badge);
    }
}

// Check backend status automatically
checkBackendStatus();

// Helper to get token
function getHeaders() {
    const token = localStorage.getItem('oecsms_token');
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
    };
}

// Universal API wrapper
const API = {
    // Auth endpoints
    async loginWithGoogle(credential) {
        if (isMockMode) {
            // Mock: treat credential as email and map to demo users
            const email = credential; // In mock, just use credential string
            // Simple mock mapping: manager@example.com -> manager, assistant@example.com -> assistant
            const mockUser = email.includes('manager') ? { role: 'Manager', username: 'manager', fullName: 'System Manager', token: 'mock_jwt_token_1' } : { role: 'Assistant', username: 'assistant', fullName: 'Alice Assistant', token: 'mock_jwt_token_2' };
            localStorage.setItem('oecsms_token', mockUser.token);
            localStorage.setItem('oecsms_current_user', JSON.stringify(mockUser));
            return { success: true, data: mockUser };
        } else {
            try {
                const res = await fetch(`${BACKEND_URL}/auth/google`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ credential })
                });
                const data = await res.json();
                if (data.success && data.data) {
                    localStorage.setItem('oecsms_token', data.data.token);
                    localStorage.setItem('oecsms_current_user', JSON.stringify(data.data));
                }
                return data;
            } catch (e) {
                console.error('Google login failed', e);
                return { success: false, message: 'Google login error' };
            }
        }
    },
        async login(username, password) {
            if (isMockMode) {
                const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
                const user = users.find(u => u.username.toLowerCase() === username.toLowerCase() && u.passwordHash === password);
                if (user && user.isActive) {
                    const token = "mock_jwt_token_" + user.userId;
                    localStorage.setItem('oecsms_token', token);
                    localStorage.setItem('oecsms_current_user', JSON.stringify(user));
                    return { success: true, data: { token, username: user.username, fullName: user.fullName, role: user.role, userId: user.userId }, message: 'Success' };
                }
                return { success: false, message: 'Invalid credentials or inactive account' };
            } else {
                try {
                    const res = await fetch(`${BACKEND_URL}/auth/login`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ username, password })
                    });

                    if (!res.ok && res.status >= 500) {
                        throw new Error('Database or Server Error');
                    }

                    const data = await res.json();

                    if (data && !data.success && data.message && (data.message.toLowerCase().includes('mysql') || data.message.toLowerCase().includes('connect') || data.message.toLowerCase().includes('database'))) {
                        throw new Error(data.message);
                    }

                    if (data.success && data.data) {
                        localStorage.setItem('oecsms_token', data.data.token);
                        localStorage.setItem('oecsms_current_user', JSON.stringify(data.data));
                    }
                    return data;
                } catch (e) {
                    console.warn("Backend login failed due to database or connection error. Falling back to Mock Mode.", e);
                    isMockMode = true;
                    initializeMockStorage();
                    showModeIndicator();
                    return await this.login(username, password);
                }
            }
        },


    getCurrentUser() {
        if (isMockMode) {
            return JSON.parse(localStorage.getItem('oecsms_current_user') || 'null');
        } else {
            const u = localStorage.getItem('oecsms_current_user');
            return u ? JSON.parse(u) : null;
        }
    },

    logout() {
        localStorage.removeItem('oecsms_token');
        localStorage.removeItem('oecsms_current_user');
        if (!isMockMode) {
            fetch(`${BACKEND_URL}/auth/logout`, { method: 'POST', headers: getHeaders() }).catch(() => {});
        }
    },

    // User endpoints
    async getUsers() {
        if (isMockMode) {
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            return { success: true, data: users };
        } else {
            const res = await fetch(`${BACKEND_URL}/users`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async createAssistant(fullName, username, password, email, phone) {
        if (isMockMode) {
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            if (users.find(u => u.username === username)) return { success: false, message: 'Username exists' };
            const newId = users.length + 1;
            const newUser = { userId: newId, username, passwordHash: password, fullName, role: 'Assistant', email, phone, isActive: true, managerId: 1 };
            users.push(newUser);
            localStorage.setItem('oecsms_users', JSON.stringify(users));
            return { success: true, message: 'Assistant created' };
        } else {
            try {
                const res = await fetch(`${BACKEND_URL}/users`, {
                    method: 'POST',
                    headers: getHeaders(),
                    body: JSON.stringify({ FullName: fullName, Username: username, Password: password, Email: email, Phone: phone })
                });
                if (!res.ok && res.status >= 500) throw new Error('Server Error');
                const data = await res.json();
                if (data && !data.success && data.message && (data.message.toLowerCase().includes('mysql') || data.message.toLowerCase().includes('database'))) throw new Error(data.message);
                return data;
            } catch (e) {
                console.warn("Backend unavailable. Creating assistant in mock mode.", e);
                isMockMode = true;
                initializeMockStorage();
                showModeIndicator();
                return await this.createAssistant(fullName, username, password, email, phone);
            }
        }
    },

    async toggleUserStatus(userId, isActive) {
        if (isMockMode) {
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            const user = users.find(u => u.userId === userId);
            if (user) user.isActive = isActive;
            localStorage.setItem('oecsms_users', JSON.stringify(users));
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/users/${userId}/status`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify(isActive)
            });
            return await res.json();
        }
    },

    async getPerformance(userId) {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const scores = JSON.parse(localStorage.getItem('oecsms_conduct_scores') || '[]');
            const escalations = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');

            const userTasks = tasks.filter(t => t.assignedToId === userId);
            const userReqs = reqs.filter(r => r.assistantId === userId);
            const userScores = scores.filter(s => s.assistantId === userId);
            const userEscalations = escalations.filter(c => c.assistantId === userId);

            const totalTasks = userTasks.length;
            const completedTasks = userTasks.filter(t => t.status === 'Completed').length;
            const overdueTasks = userTasks.filter(t => t.status !== 'Completed' && new Date(t.deadline) < new Date()).length;
            const served = userReqs.filter(r => r.status === 'Completed').length;
            const avgRating = userScores.length ? (userScores.reduce((acc, curr) => acc + curr.rating, 0) / userScores.length) : 5.0;

            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            const name = users.find(u => u.userId === userId)?.fullName || 'Assistant';

            return {
                success: true,
                data: {
                    userId,
                    fullName: name,
                    totalTasksAssigned: totalTasks,
                    tasksCompleted: completedTasks,
                    tasksOverdue: overdueTasks,
                    customersServed: served,
                    averageRating: Number(avgRating.toFixed(1)),
                    totalEscalations: userEscalations.length
                }
            };
        } else {
            const res = await fetch(`${BACKEND_URL}/users/${userId}/performance`, { headers: getHeaders() });
            return await res.json();
        }
    },

    // Tasks endpoints
    async getTasks() {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            return { success: true, data: tasks };
        } else {
            const res = await fetch(`${BACKEND_URL}/tasks`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async createTask(title, description, priority, assignedToId, deadline, notes) {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            const assignee = users.find(u => u.userId === Number(assignedToId));
            const newId = tasks.length + 1;
            const newTask = {
                taskId: newId,
                title,
                description,
                priority,
                status: 'Pending',
                assignedToId: Number(assignedToId),
                assignedToName: assignee ? assignee.fullName : 'Unknown',
                assignedById: 1,
                assignedByName: 'System Manager',
                deadline,
                createdAt: new Date().toISOString(),
                notes
            };
            tasks.push(newTask);
            localStorage.setItem('oecsms_tasks', JSON.stringify(tasks));
            
            // Add notification
            this.addMockNotification(Number(assignedToId), 'New Task Assigned', `Task: ${title}`, 'TaskUpdate', newId);

            return { success: true, data: newTask };
        } else {
            try {
                const res = await fetch(`${BACKEND_URL}/tasks`, {
                    method: 'POST',
                    headers: getHeaders(),
                    body: JSON.stringify({ title, description, priority, assignedToId, deadline, notes })
                });
                // If backend returns non-2xx, treat as error
                if (!res.ok) {
                    console.warn('Backend task creation failed, status:', res.status);
                    // fallback to mock storage
                    isMockMode = true;
                    initializeMockStorage();
                    // recurse to mock path
                    return await API.createTask(title, description, priority, assignedToId, deadline, notes);
                }
                return await res.json();
            } catch (e) {
                console.error('Error contacting backend for task creation:', e);
                // fallback to mock mode
                isMockMode = true;
                initializeMockStorage();
                return await API.createTask(title, description, priority, assignedToId, deadline, notes);
            }
        }
    },

    async updateTaskStatus(taskId, status, note = '') {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            const task = tasks.find(t => t.taskId === taskId);
            if (task) {
                const old = task.status;
                task.status = status;
                if (status === 'Completed') task.completedAt = new Date().toISOString();
                localStorage.setItem('oecsms_tasks', JSON.stringify(tasks));

                // Log audit
                const logs = JSON.parse(localStorage.getItem('oecsms_task_logs') || '[]');
                logs.push({ logId: logs.length + 1, taskId, changedByName: 'Assistant', oldStatus: old, newStatus: status, changeNote: note || `Changed status to ${status}`, changedAt: new Date().toISOString() });
                localStorage.setItem('oecsms_task_logs', JSON.stringify(logs));

                // Notify manager
                this.addMockNotification(1, 'Task Status Changed', `Task '${task.title}' changed to ${status}`, 'TaskUpdate', taskId);
                
                // Trigger real-time SignalR simulated call if set
                if (window.onSignalRMessage) {
                    window.onSignalRMessage({ title: 'Task Status Changed', message: `Task '${task.title}' changed to ${status}`, type: 'TaskUpdate', relatedEntityId: taskId });
                }
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/tasks/${taskId}/status`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify({ status, note })
            });
            return await res.json();
        }
    },

    async getTaskAudit(taskId) {
        if (isMockMode) {
            const logs = JSON.parse(localStorage.getItem('oecsms_task_logs') || '[]');
            return { success: true, data: logs.filter(l => l.taskId === taskId) };
        } else {
            const res = await fetch(`${BACKEND_URL}/tasks/${taskId}/audit`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async reassignTask(taskId, newAssigneeId) {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');
            const task = tasks.find(t => t.taskId === taskId);
            const assignee = users.find(u => u.userId === Number(newAssigneeId));
            if (task && assignee) {
                task.assignedToId = Number(newAssigneeId);
                task.assignedToName = assignee.fullName;
                task.status = 'Pending';
                localStorage.setItem('oecsms_tasks', JSON.stringify(tasks));
                this.addMockNotification(Number(newAssigneeId), 'Task Reassigned', `You have been reassigned: ${task.title}`, 'TaskUpdate', taskId);
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/tasks/${taskId}/reassign`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify(newAssigneeId)
            });
            return await res.json();
        }
    },

    async deleteTask(taskId) {
        if (isMockMode) {
            const tasks = JSON.parse(localStorage.getItem('oecsms_tasks') || '[]');
            const taskIndex = tasks.findIndex(t => t.taskId === taskId);
            if (taskIndex !== -1) {
                tasks[taskIndex].status = 'Cancelled';
                localStorage.setItem('oecsms_tasks', JSON.stringify(tasks));
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/tasks/${taskId}`, { method: 'DELETE', headers: getHeaders() });
            return await res.json();
        }
    },

    // Customer & Queue endpoints
    async registerCustomer(fullName, phone, email, serviceDescription, assignedAssistantId) {
        if (isMockMode) {
            const customers = JSON.parse(localStorage.getItem('oecsms_customers') || '[]');
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const users = JSON.parse(localStorage.getItem('oecsms_users') || '[]');

            const newCustId = customers.length + 101;
            const newReqId = reqs.length + 501;
            const queueNo = customers.filter(c => c.visitDate === new Date().toISOString().split('T')[0]).length + 1;

            const newCustomer = { customerId: newCustId, fullName, phone, email, visitDate: new Date().toISOString().split('T')[0], arrivalTime: new Date().toISOString(), queueNumber: queueNo };
            customers.push(newCustomer);
            localStorage.setItem('oecsms_customers', JSON.stringify(customers));

            const assistant = users.find(u => u.userId === Number(assignedAssistantId));

            const newRequest = {
                requestId: newReqId,
                customerId: newCustId,
                customerName: fullName,
                queueNumber: queueNo,
                assistantId: Number(assignedAssistantId),
                assistantName: assistant ? assistant.fullName : 'Alice Assistant',
                serviceDescription,
                status: 'Waiting',
                createdAt: new Date().toISOString()
            };
            reqs.push(newRequest);
            localStorage.setItem('oecsms_requests', JSON.stringify(reqs));

            // Notify assistant
            this.addMockNotification(Number(assignedAssistantId), 'Customer Arrival', `${fullName} (Queue #${queueNo}) is waiting.`, 'CustomerArrival', newReqId);
            
            if (window.onSignalRMessage) {
                window.onSignalRMessage({ title: 'Customer Waiting', message: `${fullName} has joined the queue.`, type: 'CustomerArrival', relatedEntityId: newReqId });
            }

            return { success: true, data: { customerId: newCustId, queueNumber: queueNo, estimatedWaitTimeMinutes: (queueNo - 1) * 10, requestId: newReqId } };
        } else {
            const res = await fetch(`${BACKEND_URL}/customers/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName, phone, email, serviceDescription, assignedAssistantId })
            });
            return await res.json();
        }
    },

    async getQueue() {
        if (isMockMode) {
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const active = reqs.filter(r => r.status === 'Waiting' || r.status === 'InService');
            return { success: true, data: active };
        } else {
            const res = await fetch(`${BACKEND_URL}/customers/queue`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async getServiceRequests() {
        if (isMockMode) {
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            return { success: true, data: reqs };
        } else {
            const res = await fetch(`${BACKEND_URL}/service-requests`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async updateRequestStatus(requestId, status, resolutionNote = '') {
        if (isMockMode) {
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const req = reqs.find(r => r.requestId === requestId);
            if (req) {
                req.status = status;
                if (status === 'InService') req.serviceStartTime = new Date().toISOString();
                if (['Completed', 'Referred', 'Unresolved'].includes(status)) {
                    req.serviceEndTime = new Date().toISOString();
                    req.resolutionNote = resolutionNote;
                }
                localStorage.setItem('oecsms_requests', JSON.stringify(reqs));

                if (window.onSignalRMessage) {
                    window.onSignalRMessage({ title: 'Queue Update', message: `Queue state changed`, type: 'QueueUpdate' });
                }
            }
            return { success: true, data: req };
        } else {
            const res = await fetch(`${BACKEND_URL}/service-requests/${requestId}/status`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify({ status, resolutionNote })
            });
            return await res.json();
        }
    },

    async submitFeedback(requestId, rating, feedback) {
        if (isMockMode) {
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const req = reqs.find(r => r.requestId === requestId);
            if (req) {
                req.customerRating = rating;
                req.customerFeedback = feedback;
                localStorage.setItem('oecsms_requests', JSON.stringify(reqs));

                // Log conduct score
                const scores = JSON.parse(localStorage.getItem('oecsms_conduct_scores') || '[]');
                scores.push({ scoreId: scores.length + 1, assistantId: req.assistantId, requestId, rating, managerNote: '', recordedAt: new Date().toISOString() });
                localStorage.setItem('oecsms_conduct_scores', JSON.stringify(scores));

                // Trigger alert for manager if low rating
                if (rating < 3) {
                    this.addMockNotification(1, 'Low Rating Alert', `Assistant received ${rating} stars. Feedback: ${feedback}`, 'SystemAlert', requestId);
                    if (window.onSignalRMessage) {
                        window.onSignalRMessage({ title: 'Low Rating Alert', message: `Low rating alert submitted.`, type: 'SystemAlert', relatedEntityId: requestId });
                    }
                }
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/service-requests/${requestId}/rating`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ rating, feedback })
            });
            return await res.json();
        }
    },

    // Communication (Escalations)
    async createContactRequest(requestId, customerMessage) {
        if (isMockMode) {
            const contacts = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');
            const reqs = JSON.parse(localStorage.getItem('oecsms_requests') || '[]');
            const req = reqs.find(r => r.requestId === requestId);

            const newId = contacts.length + 1;
            const newContact = {
                contactRequestId: newId,
                requestId,
                customerName: req ? req.customerName : 'Unknown',
                assistantName: req ? req.assistantName : 'Alice Assistant',
                assistantId: req ? req.assistantId : 2,
                customerMessage,
                assistantNote: '',
                status: 'Pending',
                createdAt: new Date().toISOString()
            };
            contacts.push(newContact);
            localStorage.setItem('oecsms_contact_requests', JSON.stringify(contacts));

            // Notify assistant
            this.addMockNotification(req ? req.assistantId : 2, 'Contact Manager Requested', `Customer Escalation request #${newId}`, 'ContactRequest', newId);
            if (window.onSignalRMessage) {
                window.onSignalRMessage({ title: 'Contact Manager Requested', message: `Customer wants to speak to Manager.`, type: 'ContactRequest', relatedEntityId: newId });
            }

            return { success: true, data: newContact };
        } else {
            const res = await fetch(`${BACKEND_URL}/contact-requests`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ requestId, customerMessage })
            });
            return await res.json();
        }
    },

    async getContactRequests() {
        if (isMockMode) {
            const contacts = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');
            return { success: true, data: contacts };
        } else {
            const res = await fetch(`${BACKEND_URL}/contact-requests`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async forwardContactRequest(contactRequestId, assistantNote) {
        if (isMockMode) {
            const contacts = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');
            const contact = contacts.find(c => c.contactRequestId === contactRequestId);
            if (contact) {
                contact.assistantNote = assistantNote;
                contact.status = 'Forwarded';
                contact.forwardedAt = new Date().toISOString();
                localStorage.setItem('oecsms_contact_requests', JSON.stringify(contacts));

                // Notify manager
                this.addMockNotification(1, 'Escalation Forwarded', `Forwarded from Assistant. Message: ${contact.customerMessage}`, 'ContactRequest', contactRequestId);
                if (window.onSignalRMessage) {
                    window.onSignalRMessage({ title: 'Escalation Forwarded', message: `Escalation request forwarded.`, type: 'ContactRequest', relatedEntityId: contactRequestId });
                }
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/contact-requests/${contactRequestId}/forward`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify({ assistantNote })
            });
            return await res.json();
        }
    },

    async replyContactRequest(contactRequestId, replyMessage) {
        if (isMockMode) {
            const contacts = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');
            const contact = contacts.find(c => c.contactRequestId === contactRequestId);
            if (contact) {
                contact.managerReply = replyMessage;
                contact.status = 'Replied';
                contact.repliedAt = new Date().toISOString();
                localStorage.setItem('oecsms_contact_requests', JSON.stringify(contacts));

                // Notify assistant
                this.addMockNotification(contact.assistantId || 2, 'Manager Reply Received', `Reply: ${replyMessage}`, 'ContactRequest', contactRequestId);
                if (window.onSignalRMessage) {
                    window.onSignalRMessage({ title: 'Manager Replied', message: `Reply: ${replyMessage}`, type: 'ContactRequest', relatedEntityId: contactRequestId });
                }
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/contact-requests/${contactRequestId}/reply`, {
                method: 'PATCH',
                headers: getHeaders(),
                body: JSON.stringify({ replyMessage })
            });
            return await res.json();
        }
    },

    async closeContactRequest(contactRequestId) {
        if (isMockMode) {
            const contacts = JSON.parse(localStorage.getItem('oecsms_contact_requests') || '[]');
            const contact = contacts.find(c => c.contactRequestId === contactRequestId);
            if (contact) {
                contact.status = 'Closed';
                localStorage.setItem('oecsms_contact_requests', JSON.stringify(contacts));
            }
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/contact-requests/${contactRequestId}/close`, {
                method: 'PATCH',
                headers: getHeaders()
            });
            return await res.json();
        }
    },

    // Notifications
    async getNotifications() {
        if (isMockMode) {
            const notifications = JSON.parse(localStorage.getItem('oecsms_notifications') || '[]');
            const user = this.getCurrentUser();
            const filtered = notifications.filter(n => n.recipientUserId === (user ? user.userId : 2));
            return { success: true, data: filtered };
        } else {
            const res = await fetch(`${BACKEND_URL}/notifications`, { headers: getHeaders() });
            return await res.json();
        }
    },

    async markNotificationRead(notificationId) {
        if (isMockMode) {
            const notifications = JSON.parse(localStorage.getItem('oecsms_notifications') || '[]');
            const n = notifications.find(notif => notif.notificationId === notificationId);
            if (n) n.isRead = true;
            localStorage.setItem('oecsms_notifications', JSON.stringify(notifications));
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/notifications/${notificationId}/read`, {
                method: 'PATCH',
                headers: getHeaders()
            });
            return await res.json();
        }
    },

    async markAllNotificationsRead() {
        if (isMockMode) {
            const notifications = JSON.parse(localStorage.getItem('oecsms_notifications') || '[]');
            const user = this.getCurrentUser();
            notifications.forEach(n => {
                if (n.recipientUserId === (user ? user.userId : 2)) n.isRead = true;
            });
            localStorage.setItem('oecsms_notifications', JSON.stringify(notifications));
            return { success: true };
        } else {
            const res = await fetch(`${BACKEND_URL}/notifications/read-all`, {
                method: 'PATCH',
                headers: getHeaders()
            });
            return await res.json();
        }
    },

    // Mock Helper
    addMockNotification(recipientUserId, title, message, type, relatedEntityId = null) {
        const notifications = JSON.parse(localStorage.getItem('oecsms_notifications') || '[]');
        notifications.push({
            notificationId: notifications.length + 1,
            recipientUserId,
            title,
            message,
            type,
            isRead: false,
            createdAt: new Date().toISOString(),
            relatedEntityId
        });
        localStorage.setItem('oecsms_notifications', JSON.stringify(notifications));
    }
};

window.API = API;
