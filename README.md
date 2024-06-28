# Web-Server

A simple web server which can send responses to the most well-known HTTP methods (GET and POST), in C#. Then we will make this server accessible from the internet.

GET
What happens when we write an address into the address bar of our web browser and hit enter? (We mostly don't specify a port number although it is required for TCP/IP, because it has a default value for http and it is 80. We don't have to specify it if it is 80.)

GET / HTTP/1.1\r\n
Host: atasoyweb.net\r\n
User-Agent: Mozilla/5.0 (Windows NT 6.1; rv:14.0) Gecko/20100101 Firefox/14.0.1\r\n
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n
Accept-Language: tr-tr,tr;q=0.8,en-us;q=0.5,en;q=0.3\r\n
Accept-Encoding: gzip, deflate\r\n
Connection: keep-alive\r\n\r\n
This is the GET request which is sent by our browser to the server using TCP/IP. This means the browser requests the server to send the contents of "/" from the root folder of "atasoyweb.net".

We (or browsers) can add more headers. But the most simplified version of this request is below:

GET / HTTP/1.1\r\n
Host: atasoyweb.net\r\n\r\n 

POST
POST requests are similar to GET requests. In a GET request, variables are appended to the URLs using ? character. But in a POST request, variables are appended to the end of the request after 2 line break characters and total length (content-length) is specified.

POST /index.html HTTP/1.1\r\n
Host: atasoyweb.net\r\n
User-Agent: Mozilla/5.0 (Windows NT 6.1; rv:15.0) Gecko/20100101 Firefox/15.0.1\r\n
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n
Accept-Language: tr-tr,tr;q=0.8,en-us;q=0.5,en;q=0.3\r\n
Accept-Encoding: gzip, deflate\r\n
Connection: keep-alive\r\n
Referer: http://atasoyweb.net/\r\n
Content-Type: application/x-www-form-urlencoded\r\n
Content-Length: 35\r\n\r\n
variable1=value1&variable2=value2
Simplified version of this request:

POST /index.html HTTP/1.1\r\n
Host: atasoyweb.net\r\n
Content-Length: 35\r\n\r\n
variable1=value1&variable2=value2 

Responses
When a request is received by the server, it is parsed and a response with a status code is returned:

HTTP/1.1 200 OK\r\n
Server: Apache/1.3.3.7 (Unix) (Red-Hat/Linux)\r\n
Content-Length: {content_length}\r\n
Connection: close\r\n
Content-Type: text/html; charset=UTF-8\r\n\r\n
the content of which length is equal to {content_length}
This is the response header. "200 OK" means everything is OK, requested content will be returned. There are many status codes. We will use just 200, 501 and 404:

"501 Not Implemented": Method is not implemented. We will implement only GET and POST. So, we will send response with this code for all other methods.
"404 Not Found": Requested content is not found.

If we want our server to be available even if a response is being sent to another client at that time, we must create new threads for every request. Thus, every thread handles a single request and exits after it completes its mission. (Multithreading also speeds up page loadings, because if we request a page that uses CSS and includes images, different GET requests are sent for every image and CSS file.)

# Server Class:

## Fields:
    running: A boolean indicating if the server is running.
    timeout: An integer representing the timeout duration for data transfers.
    charEncoder: An Encoding object used for string conversions.
    serverSocket: A Socket object representing the server socket.
    contentPath: A string representing the root path of the content to be served.
    extensions: A dictionary mapping file extensions to MIME types.
## Methods:
    Start: Initializes and starts the server.
    Stop: Stops the server.
    HandleTheRequest: Handles incoming client requests.
    NotImplemented: Sends a 501 Not Implemented response to the client.
    NotFound: Sends a 404 Not Found response to the client.
    SendOkResponse: Sends a 200 OK response to the client with the specified content.
    SendResponse: Sends a response to the client with the specified content, response code, and content type.
## Program Class:
    Main Method: Defines the IP address, port, maximum number of connections, and content path. It creates an instance of the server, starts it, and waits for a key press to stop the server.

# To Compile and Run
## Save the File:
    Save the changes to Program.cs.

## Build the Project:
    In the command line, navigate to the project directory and run:
    dotnet build

## Run the Project:
    After building successfully, run the project using:
    dotnet run

## Access the Server:
    Open a web browser and navigate to http://127.0.0.1:8080.

## Stop the Server:
    To stop the server, press any key in the terminal.