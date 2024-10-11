const WebSocket = require('ws');

// Create a WebSocket client that connects to the server
const ws = new WebSocket('ws://localhost:8080/ws/');

// Event when the connection is opened
ws.on('open', () => {
    console.log('Connected to the server');

    // Send a message to the server
    ws.send('Hello, Server!');
});

// Event when a message is received from the server
ws.on('message', (message) => {
    console.log(`Received from server: ${message}`);
});
