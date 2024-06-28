using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class Server
{
    // Indicates whether the server is currently running
    private bool running = false;

    // Timeout duration for data transfers (in milliseconds)
    private int timeout = 8000;

    // Character encoding used for string conversions
    private Encoding charEncoder = Encoding.UTF8;

    // The main server socket
    private Socket? serverSocket = null;

    // Root path of the content to be served
    private string contentPath = string.Empty;

    // Dictionary to map file extensions to their MIME types
    private Dictionary<string, string> extensions = new Dictionary<string, string>()
    {
        { "htm", "text/html" },
        { "html", "text/html" },
        { "xml", "text/xml" },
        { "txt", "text/plain" },
        { "css", "text/css" },
        { "png", "image/png" },
        { "gif", "image/gif" },
        { "jpg", "image/jpg" },
        { "jpeg", "image/jpeg" },
        { "zip", "application/zip"}
    };

    // Starts the server with the specified IP address, port, maximum number of connections, and content path
    public bool Start(IPAddress ipAddress, int port, int maxConnections, string contentPath)
    {
        // If the server is already running, return false
        if (running) return false;

        try
        {
            // Initialize the server socket (TCP/IP, IPv4)
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the specified IP address and port
            serverSocket.Bind(new IPEndPoint(ipAddress, port));

            // Set the socket to listen for incoming connections
            serverSocket.Listen(maxConnections);

            // Set receive and send timeouts for the socket
            serverSocket.ReceiveTimeout = timeout;
            serverSocket.SendTimeout = timeout;

            // Set the running flag to true
            running = true;

            // Set the content path
            this.contentPath = contentPath;
        }
        catch
        {
            // If any exception occurs, return false
            return false;
        }

        // Start a new thread to listen for connection requests and handle them
        Thread requestListenerThread = new Thread(() =>
        {
            while (running)
            {
                try
                {
                    // Accept incoming client connections
                    Socket clientSocket = serverSocket.Accept();

                    // Start a new thread to handle the client's request
                    Thread requestHandlerThread = new Thread(() =>
                    {
                        // Set receive and send timeouts for the client socket
                        clientSocket.ReceiveTimeout = timeout;
                        clientSocket.SendTimeout = timeout;

                        try
                        {
                            // Handle the client's request
                            HandleTheRequest(clientSocket);
                        }
                        catch
                        {
                            // If any exception occurs, close the client socket
                            try { clientSocket.Close(); } catch { }
                        }
                    });

                    // Start the request handler thread
                    requestHandlerThread.Start();
                }
                catch { }
            }
        });

        // Start the request listener thread
        requestListenerThread.Start();

        return true;
    }

    // Stops the server
    public void Stop()
    {
        if (running)
        {
            // Set the running flag to false
            running = false;

            // Close the server socket if it is not null
            try { serverSocket?.Close(); }
            catch { }

            // Set the server socket to null
            serverSocket = null;
        }
    }

    // Handles the client's request
    private void HandleTheRequest(Socket clientSocket)
    {
        // Buffer to store the received data (10 KB)
        byte[] buffer = new byte[10240];

        // Receive the client's request
        int receivedByteCount = clientSocket.Receive(buffer);

        // Convert the received bytes to a string
        string requestString = charEncoder.GetString(buffer, 0, receivedByteCount);

        // Parse the HTTP method from the request string
        string httpMethod = requestString.Substring(0, requestString.IndexOf(" "));

        // Parse the requested URL from the request string
        int start = requestString.IndexOf(httpMethod) + httpMethod.Length + 1;
        int length = requestString.LastIndexOf("HTTP") - start - 1;
        string requestedUrl = requestString.Substring(start, length);

        string requestedFile;

        // If the request method is GET or POST, process the request
        if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
        {
            requestedFile = requestedUrl.Split('?')[0];
        }
        else
        {
            // If the request method is not supported, send a 501 Not Implemented response
            NotImplemented(clientSocket);
            return;
        }

        // Sanitize the requested file path to prevent directory traversal attacks
        requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");

        start = requestedFile.LastIndexOf('.') + 1;

        // If the requested file has an extension, process it
        if (start > 0)
        {
            length = requestedFile.Length - start;
            string extension = requestedFile.Substring(start, length);

            // If the file extension is supported, send the file with the appropriate MIME type
            if (extensions.ContainsKey(extension))
            {
                SendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
            }
            else
            {
                // If the file extension is not supported, send a 404 Not Found response
                NotFound(clientSocket);
            }
        }
        else
        {
            // If the file is not specified, try to send index.htm or index.html
            if (requestedFile.Substring(length - 1, 1) != @"\")
                requestedFile += @"\";

            if (File.Exists(contentPath + requestedFile + "index.htm"))
                SendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile + "index.htm"), "text/html");
            else if (File.Exists(contentPath + requestedFile + "index.html"))
                SendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile + "index.html"), "text/html");
            else
                NotFound(clientSocket);
        }
    }

    // Sends a 501 Not Implemented response to the client
    private void NotImplemented(Socket clientSocket)
    {
        SendResponse(clientSocket, "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body><h2>Simple Web Server</h2><div>501 - Method Not Implemented</div></body></html>", "501 Not Implemented", "text/html");
    }

    // Sends a 404 Not Found response to the client
    private void NotFound(Socket clientSocket)
    {
        SendResponse(clientSocket, "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body><h2>Simple Web Server</h2><div>404 - Not Found</div></body></html>", "404 Not Found", "text/html");
    }

    // Sends a 200 OK response to the client with the specified content
    private void SendOkResponse(Socket clientSocket, byte[] content, string contentType)
    {
        SendResponse(clientSocket, content, "200 OK", contentType);
    }

    // Sends a response to the client with the specified string content, response code, and content type
    private void SendResponse(Socket clientSocket, string content, string responseCode, string contentType)
    {
        byte[] byteContent = charEncoder.GetBytes(content);
        SendResponse(clientSocket, byteContent, responseCode, contentType);
    }

    // Sends a response to the client with the specified byte array content, response code, and content type
    private void SendResponse(Socket clientSocket, byte[] content, string responseCode, string contentType)
    {
        try
        {
            // Create the HTTP response header
            byte[] header = charEncoder.GetBytes(
                "HTTP/1.1 " + responseCode + "\r\n" +
                "Server: Simple Web Server\r\n" +
                "Content-Length: " + content.Length.ToString() + "\r\n" +
                "Connection: close\r\n" +
                "Content-Type: " + contentType + "\r\n\r\n");

            // Send the header and content to the client
            clientSocket.Send(header);
            clientSocket.Send(content);

            // Close the client socket
            clientSocket.Close();
        }
        catch { }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Define the IP address, port, maximum number of connections, and content path
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 8080;
        int maxConnections = 10;
        string contentPath = @"C:\path\to\your\content";

        // Create an instance of the server
        Server server = new Server();

        // Start the server
        bool serverStarted = server.Start(ipAddress, port, maxConnections, contentPath);

        if (serverStarted)
        {
            Console.WriteLine("Server started successfully.");
        }
        else
        {
            Console.WriteLine("Failed to start the server.");
        }

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        // Stop the server
        server.Stop();
        Console.WriteLine("Server stopped.");
    }
}

