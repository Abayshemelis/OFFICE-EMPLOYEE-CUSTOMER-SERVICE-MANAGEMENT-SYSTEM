let currentRequestId = null;
let currentRating = 5;
let statusCheckInterval = null;
let selectedNeedText = '';

function selectNeed(element, needText) {
    // Deselect all
    document.querySelectorAll('.quick-need-btn').forEach(btn => btn.classList.remove('selected'));
    // Select current
    element.classList.add('selected');
    selectedNeedText = needText;
    document.getElementById('cust-desc').value = ''; // Clear custom field if quick need chosen
}

document.getElementById('cust-desc').oninput = () => {
    // Deselect quick need if user types custom
    document.querySelectorAll('.quick-need-btn').forEach(btn => btn.classList.remove('selected'));
    selectedNeedText = '';
};

// Arrival submit
document.getElementById('register-form').onsubmit = async (e) => {
    e.preventDefault();
    const name = document.getElementById('cust-name').value;
    const phone = document.getElementById('cust-phone').value;
    const desc = selectedNeedText || document.getElementById('cust-desc').value || 'General Inquiry';
    const assistantId = document.getElementById('asst-id').value;

    const res = await API.registerCustomer(name, phone, '', desc, assistantId);
    if (res.success) {
        currentRequestId = res.data.requestId;
        document.getElementById('ticket-number').innerText = '#' + String(res.data.queueNumber).padStart(2, '0');
        document.getElementById('est-time').innerText = res.data.estimatedWaitTimeMinutes + ' mins';
        
        document.getElementById('step-register').style.display = 'none';
        document.getElementById('step-ticket').style.display = 'block';

        Utils.toast('Successfully registered. Queue No #' + res.data.queueNumber, 'Kiosk Check-In', 'success');

        // Start checking status updates
        startStatusCheck();
    } else {
        Utils.toast(res.message, 'Registration Failed', 'danger');
    }
};

function startStatusCheck() {
    if (statusCheckInterval) clearInterval(statusCheckInterval);
    
    statusCheckInterval = setInterval(async () => {
        if (!currentRequestId) return;
        const res = await API.getServiceRequests();
        if (res.success) {
            const req = res.data.find(r => r.requestId === currentRequestId);
            if (req) {
                updateQueueStatusUI(req.status);
            }
        }
    }, 4000);
}

function updateQueueStatusUI(status) {
    const statusText = document.getElementById('queue-status-text');
    
    if (status === 'InService') {
        statusText.innerText = 'Being Served';
        statusText.style.color = '#3b82f6';
    } else if (['Completed', 'Referred', 'Unresolved'].includes(status)) {
        clearInterval(statusCheckInterval);
        document.getElementById('step-ticket').style.display = 'none';
        document.getElementById('step-feedback').style.display = 'block';
        setRating(5); // Default to 5 stars
    }
}

// Escalate modal
function openEscalateModal() {
    document.getElementById('escalate-modal').classList.add('active');
}

function closeEscalateModal() {
    document.getElementById('escalate-modal').classList.remove('active');
}

document.getElementById('escalate-form').onsubmit = async (e) => {
    e.preventDefault();
    const msg = document.getElementById('escalate-msg').value;

    const res = await API.createContactRequest(currentRequestId, msg);
    if (res.success) {
        Utils.toast('Request forwarded to assistant. The manager will reply through the assistant.', 'Escalation Sent', 'success');
        closeEscalateModal();
        document.getElementById('escalate-btn').disabled = true;
        document.getElementById('escalate-btn').innerText = '⏳ Escalation Pending';
    } else {
        Utils.toast(res.message, 'Escalation Failed', 'danger');
    }
};

// Stars rating
function setRating(rating) {
    currentRating = rating;
    const stars = document.querySelectorAll('.star');
    stars.forEach((star, index) => {
        if (index < rating) {
            star.classList.add('active');
        } else {
            star.classList.remove('active');
        }
    });
}

// Feedback submit
document.getElementById('feedback-form').onsubmit = async (e) => {
    e.preventDefault();
    const feedback = document.getElementById('feedback-comments').value;

    const res = await API.submitFeedback(currentRequestId, currentRating, feedback);
    if (res.success) {
        Utils.toast('Thank you for your rating and comments!', 'Feedback Submitted', 'success');
        setTimeout(() => {
            resetKiosk();
        }, 1500);
    } else {
        Utils.toast(res.message, 'Failed to submit feedback', 'danger');
    }
};

function simulateServiceFinish() {
    if (currentRequestId) {
        API.updateRequestStatus(currentRequestId, 'Completed', 'Simulated finish in Kiosk');
        updateQueueStatusUI('Completed');
    }
}

function resetKiosk() {
    if (statusCheckInterval) clearInterval(statusCheckInterval);
    currentRequestId = null;
    currentRating = 5;
    selectedNeedText = '';
    
    // Clear forms
    document.getElementById('register-form').reset();
    document.getElementById('feedback-form').reset();
    document.getElementById('escalate-form').reset();
    document.querySelectorAll('.quick-need-btn').forEach(btn => btn.classList.remove('selected'));
    
    document.getElementById('escalate-btn').disabled = false;
    document.getElementById('escalate-btn').innerText = '⚠️ Request Manager Attention';

    document.getElementById('step-ticket').style.display = 'none';
    document.getElementById('step-feedback').style.display = 'none';
    document.getElementById('step-register').style.display = 'block';
}
