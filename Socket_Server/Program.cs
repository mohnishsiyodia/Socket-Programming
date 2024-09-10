using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    private Socket serverSocket;
    private const int PORT = 12345;
    private static Dictionary<int, Socket> clientSockets = new Dictionary<int, Socket>();
    private int ReceivedClientId;
    private static int heartbeatCounter = 0;
    private static readonly object clientLock = new object();

    public void Start()
    {
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
        serverSocket.Listen(10); // Start listening with a backlog of 10 connections

        Console.WriteLine("Server started, waiting for clients...");

        Thread heartbeatThread = new Thread(SendHeartbeat);
        heartbeatThread.Start();

        serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
    }

    private void AcceptCallback(IAsyncResult ar)
    {
        Socket clientSocket = serverSocket.EndAccept(ar);

        Thread clientThread = new Thread(() => HandleClient(clientSocket));
        clientThread.Start();

        serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
    }

    private void HandleClient(Socket handler)
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (true)
        {
            try
            {
                bytesRead = handler.Receive(buffer);
                if (bytesRead > 0)
                {
                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    dynamic jsonData = JsonConvert.DeserializeObject(receivedData);

                    int receivedClientId = jsonData.clientId;
                    string message = jsonData.message;
                    string messageType = jsonData.messageType;

                    Console.WriteLine($"Received from client {receivedClientId}: {message}: {messageType}");

                    if(messageType != "HB")
                    {
                        lock (clientLock)
                        {
                            clientSockets.Add(receivedClientId, handler);
                        }
                    }
                    
                    ReceivedClientId = receivedClientId;
                    Console.WriteLine("Dictionary entries:");
                    foreach (var pair in clientSockets)
                    {
                        Console.WriteLine($"ID: {pair.Key}, SOCKET: {pair.Value}");
                    }
                }
                else
                {
                    // If bytesRead <= 0, the client has disconnected
                    break;
                }
            }
            catch (SocketException)
            {
                // Socket exception indicates client has disconnected
                break;
            }
        }

        // Remove client from dictionary and display disconnection message
        lock (clientLock)
        {
            clientSockets.Remove(ReceivedClientId);
        }
        Console.WriteLine($"Hello, Client disconnected.");
    }
    private static void SendHeartbeat()
    {
        while (true)
        {
            Console.WriteLine("Heartbeat started");
            Thread.Sleep(60000); // Wait for 1 minute
            heartbeatCounter++;
            Console.WriteLine(heartbeatCounter);
            if (heartbeatCounter % 1 == 0)
            {
                lock (clientLock)
                {
                    var json = new { heartbeat = heartbeatCounter };
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(json);
                    byte[] data = Encoding.ASCII.GetBytes(jsonData);

                    foreach (var clientSocket in clientSockets.Values)
                    {
                        try
                        {
                            clientSocket.Send(data);
                        }
                        catch(SocketException)
                        {
                            return;
                        }
                    }
                    Console.WriteLine($"Heartbeat sent: {heartbeatCounter}");
                }
            }
        }
    }

    public void Stop()
    {
        foreach (Socket socket in clientSockets.Values)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        serverSocket.Close();
    }

    public static void Main()
    {
        Server server = new Server();
        server.Start();

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.Stop();
    }
}

