// Shared utility and visual UI functions
const Utils = {
    // Format dates cleanly
    formatDate(dateStr) {
        if (!dateStr) return 'N/A';
        const d = new Date(dateStr);
        return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    },

    // Format priority with color badges
    getPriorityBadge(priority) {
        const colors = {
            'Low': 'bg-success',
            'Medium': 'bg-warning-light',
            'High': 'bg-warning',
            'Critical': 'bg-danger'
        };
        const badgeColor = colors[priority] || 'bg-secondary';
        return `<span class="badge ${badgeColor}">${priority}</span>`;
    },

    // Format task status with badges
    getStatusBadge(status) {
        const colors = {
            'Pending': 'bg-info',
            'InProgress': 'bg-primary',
            'OnHold': 'bg-warning',
            'Completed': 'bg-success',
            'Cancelled': 'bg-danger'
        };
        const badgeColor = colors[status] || 'bg-secondary';
        // Add space in camel case
        const friendlyStatus = status.replace(/([A-Z])/g, ' $1').trim();
        return `<span class="badge ${badgeColor}">${friendlyStatus}</span>`;
    },

    // Toast notification system
    toast(message, title = 'Notification', type = 'info') {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.top = '20px';
            container.style.right = '20px';
            container.style.zIndex = '10000';
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.gap = '10px';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `custom-toast toast-${type}`;
        toast.style.background = 'rgba(255, 255, 255, 0.95)';
        toast.style.backdropFilter = 'blur(10px)';
        toast.style.borderLeft = `5px solid var(--toast-color-${type}, #2196F3)`;
        toast.style.boxShadow = '0 10px 30px rgba(0,0,0,0.15)';
        toast.style.borderRadius = '8px';
        toast.style.padding = '15px 20px';
        toast.style.width = '320px';
        toast.style.fontFamily = 'system-ui, sans-serif';
        toast.style.fontSize = '14px';
        toast.style.color = '#333';
        toast.style.display = 'flex';
        toast.style.flexDirection = 'column';
        toast.style.animation = 'slideIn 0.3s ease forwards';

        // Custom theme colors for type
        const colors = {
            info: '#2196F3',
            success: '#4CAF50',
            warning: '#FF9800',
            danger: '#F44336'
        };
        const color = colors[type] || '#2196F3';
        toast.style.setProperty(`--toast-color-${type}`, color);

        toast.innerHTML = `
            <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:5px;">
                <strong style="color:${color};font-weight:600;">${title}</strong>
                <span class="toast-close" style="cursor:pointer;font-weight:bold;color:#aaa;">&times;</span>
            </div>
            <div style="line-height:1.4;">${message}</div>
        `;

        container.appendChild(toast);

        // Bind close button
        toast.querySelector('.toast-close').onclick = () => {
            toast.style.animation = 'fadeOut 0.3s ease forwards';
            setTimeout(() => toast.remove(), 300);
        };

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (toast.parentNode) {
                toast.style.animation = 'fadeOut 0.3s ease forwards';
                setTimeout(() => toast.remove(), 300);
            }
        }, 5000);
    }
};

window.Utils = Utils;
