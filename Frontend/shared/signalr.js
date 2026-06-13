// SignalR wrapper for real-time notifications
let connection = null;

function setupSignalR(userId, onNotification, onQueueUpdate) {
    // If mock mode is active, we simulate SignalR messages in memory
    const token = localStorage.getItem('oecsms_token');
    
    // Bind to window for api.js triggers
    window.onSignalRMessage = (msg) => {
        if (msg.type === 'QueueUpdate' || msg.type === 'QueueUpdated') {
            if (onQueueUpdate) onQueueUpdate();
        } else {
            if (onNotification) onNotification(msg);
        }
    };

    // Check if SignalR script is loaded
    if (typeof signalR === 'undefined') {
        console.log("SignalR client library not loaded. Running in real-time simulation mode.");
        return;
    }

    const hubUrl = `https://localhost:7000/notificationHub?userId=${userId}`;
    
    connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveNotification", (notification) => {
        console.log("Real-time notification received via SignalR:", notification);
        if (onNotification) onNotification(notification);
    });

    connection.on("QueueUpdated", () => {
        console.log("Queue update signal received via SignalR");
        if (onQueueUpdate) onQueueUpdate();
    });

    connection.start()
        .then(() => console.log("SignalR connection established successfully."))
        .catch(err => console.warn("Failed to connect via SignalR. Updates will fall back to polling / simulation. Details:", err));
}

window.setupSignalR = setupSignalR;
