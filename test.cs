using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class Server
{
    // Is the web server currently running?
    private bool running = false;

    // Time limit for the data transfers
    private int timeout = 8000;

    // To encode string
    private Encoding charEncoder = Encoding.UTF8;

    // Server Socket
    private Socket serverSocket;

    // Root Path of our contents
    private string contentPath;

    // Content types the server supports
    private Dictionary<string, string> extensions = new Dictionary<string, string>()
    {
        //{ "extension", "content type" }
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

    // To start our server
    public bool Start(IPAddress ipAddress, int port, int maxNOfCon, string contentPath)
    {
        // If the server is already running
        if (running) return false;

        try
        {
            // TCP/IP Socket (IPv4)
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ipAddress, port));
            serverSocket.Listen(maxNOfCon);
            serverSocket.ReceiveTimeout = timeout;
            serverSocket.SendTimeout = timeout;

            running = true;
            this.contentPath = contentPath;
        }
        catch
        {
            return false;
        }

        // Thread that will listen for connection requests and create new threads to handle them
        Thread requestListenerT = new Thread(() =>
        {
            while (running)
            {
                Socket clientSocket;
                try
                {
                    clientSocket = serverSocket.Accept();

                    // Create a new thread to handle the request
                    // And continue to listen to the socket
                    Thread requestHandler = new Thread(() =>
                    {
                        clientSocket.ReceiveTimeout = timeout;
                        clientSocket.SendTimeout = timeout;
                        try
                        {
                            HandleTheRequest(clientSocket);
                        }
                        catch
                        {
                            try { clientSocket.Close(); } catch { }
                        }
                    });
                    requestHandler.Start();
                }
                catch { }
            }
        });
        requestListenerT.Start();
        return true;
    }

    // Stop the server
    public void Stop()
    {
        if (running)
        {
            running = false;
            try { serverSocket.Close(); }
            catch { }
            serverSocket = null;
        }
    }

    // Request Handler
    private void HandleTheRequest(Socket clientSocket)
    {
        // 10 kb just in case
        byte[] buffer = new byte[10240];

        // Receive the request
        int receivedBCount = clientSocket.Receive(buffer);
        string strReceived = charEncoder.GetString(buffer, 0, receivedBCount);

        // Parse method of the request
        string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

        int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
        int length = strReceived.LastIndexOf("HTTP") - start - 1;
        string requestedUrl = strReceived.Substring(start, length);

        string requestedFile;
        if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
        {
            requestedFile = requestedUrl.Split('?')[0];
        }
        else
        {
            NotImplemented(clientSocket);
            return;
        }

        requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
        start = requestedFile.LastIndexOf('.') + 1;
        if (start > 0)
        {
            length = requestedFile.Length - start;
            string extension = requestedFile.Substring(start, length);
            if (extensions.ContainsKey(extension))
            {
                // Do we support this extension? If we do we need to check the existence
                // of the file and if everything is okay send the requested file with the correct content type
                SendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
            }
            else
            {
                NotFound(clientSocket);
            }
        }
        else
        {
            // If the file is not specified try to send index.htm or index.html
            if (requestedFile.Substring(length - 1, 1) != @"\")
                requestedFile += @"\";
            if (File.Exists(contentPath + requestedFile + "index.htm"))
                SendOkResponse(clientSocket,
                  File.ReadAllBytes(contentPath + requestedFile + "index.htm"), "text/html");
            else if (File.Exists(contentPath + requestedFile + "index.html"))
                SendOkResponse(clientSocket,
                  File.ReadAllBytes(contentPath + requestedFile + "index.html"), "text/html");
            else
                NotFound(clientSocket);
        }
    }

    // Responses for different status codes
    private void NotImplemented(Socket clientSocket)
    {
        SendResponse(clientSocket, "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body><h2>Simple Web Server</h2><div>501 - Method Not Implemented</div></body></html>", "501 Not Implemented", "text/html");
    }

    private void NotFound(Socket clientSocket)
    {
        SendResponse(clientSocket, "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body><h2>Simple Web Server</h2><div>404 - Not Found</div></body></html>", "404 Not Found", "text/html");
    }

    private void SendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
    {
        SendResponse(clientSocket, bContent, "200 OK", contentType);
    }

    // Send Responses to Clients
    // For Strings
    private void SendResponse(Socket clientSocket, string strContent, string responseCode, string contentType)
    {
        byte[] bContent = charEncoder.GetBytes(strContent);
        SendResponse(clientSocket, bContent, responseCode, contentType);
    }

    // For Byte Arrays
    private void SendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
    {
        try
        {
            byte[] bHeader = charEncoder.GetBytes(
                                "HTTP/1.1 " + responseCode + "\r\n"
                              + "Server: Simple Web Server\r\n"
                              + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                              + "Connection: close\r\n"
                              + "Content-Type: " + contentType + "\r\n\r\n");
            clientSocket.Send(bHeader);
            clientSocket.Send(bContent);
            clientSocket.Close();
        }
        catch { }
    }
}

class Test
{
    static void Main(string[] args)
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 8080;
        int maxConnections = 10;
        string contentPath = @"C:\path\to\your\content";

        Server server = new Server();
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

        server.Stop();
        Console.WriteLine("Server stopped.");
    }
}
