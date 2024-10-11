using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace TrayIconAppTest
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private HttpListener httpListener;
        private bool running = true;
        private List<WebSocket> activeWebSockets = new List<WebSocket>();
        private static Form1 _instance;

        public Form1()
        {
            InitializeComponent();
            _instance = this;

            // Create a context menu for the tray icon
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, OnShow);
            trayMenu.Items.Add("Exit", null, OnExit);

            // Create the tray icon and attach the menu
            trayIcon = new NotifyIcon
            {
                Text = "WebSocket and HTTP Server",
                Icon = new Icon(SystemIcons.Application, 40, 40),
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            this.WindowState = FormWindowState.Minimized;

            toolStripStatusLabel1.Text = "WebSocket and HTTP Server";
            // Start the HTTP/WebSocket server in a background task
            Task.Run(() => StartHttpServer());
        }
        private void OnShow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void OnExit(object sender, EventArgs e)
        {
            running = false;
            httpListener?.Stop();
            trayIcon.Visible = false; // Hide the tray icon when the application exits
            Application.Exit();
        }

        private async Task StartHttpServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Prefixes.Add("http://localhost:8080/ws/");
            httpListener.Start();
            Console.WriteLine("Server started at http://localhost:8080/");

            while (running)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest && context.Request.RawUrl == "/ws/")
                {
                    _ = HandleWebSocketRequest(context); // Handle WebSocket connections
                }
                else
                {
                    await HandleHttpRequest(context); // Handle HTTP requests
                }
            }
        }

        private async Task HandleHttpRequest(HttpListenerContext context)
        {
            // Get the 'data' query string parameter from the URL
            string dataParam = context.Request.QueryString["data"];
            if (!string.IsNullOrEmpty(dataParam))
            {
                Console.WriteLine($"Received HTTP data: {dataParam}");
                await SendMessageToWebSocketClients(dataParam); // Send data to WebSocket clients
            }
            else
            {
                Console.WriteLine("No data parameter received.");
            }

            // Respond to the HTTP request
            string responseMessage = !string.IsNullOrEmpty(dataParam)
                ? $"<html><body><h1>Received data: {dataParam}</h1></body></html>"
                : "<html><body><h1>No data received</h1></body></html>";

            byte[] buffer = Encoding.UTF8.GetBytes(responseMessage);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";

            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private async Task HandleWebSocketRequest(HttpListenerContext context)
        {
            Console.WriteLine("WebSocket Request...");

            WebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = wsContext.WebSocket;

            // Add the new WebSocket to the active list
            lock (activeWebSockets)
            {
                activeWebSockets.Add(webSocket);
            }

            byte[] buffer = new byte[1024];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
                        Console.WriteLine("WebSocket closed.");

                        // Remove the WebSocket from the active list
                        lock (activeWebSockets)
                        {
                            activeWebSockets.Remove(webSocket);
                        }
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received WebSocket message: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }

        private async Task SendMessageToWebSocketClients(string message)
        {
            var jsonObject = new { data = message };
            string jsonMessage = JsonConvert.SerializeObject(jsonObject);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);

            List<Task> sendTasks = new List<Task>();
            lock (activeWebSockets)
            {
                foreach (var socket in activeWebSockets)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        sendTasks.Add(socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));
                    }
                }
            }

            await Task.WhenAll(sendTasks);
            Console.WriteLine("Message sent to all connected WebSocket clients.");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await SendMessageToWebSocketClients(textBox1.Text);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await SendMessageToWebSocketClients("12345678");
        }
    }
}
 
