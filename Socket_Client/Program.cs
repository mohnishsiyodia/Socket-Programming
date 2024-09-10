using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

class Client
{
    private static Socket clientSocket;
    private const int BUFFER_SIZE = 1024;
    private static int clientId;

    public void Start(string serverIP, int port)
    {
        Console.WriteLine("Please enter UID: ");
        int UID = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine(UID);
        if (UID >= 0)
        {
            clientId = UID;
        }
        else
        {
            Console.WriteLine("Please enter numbers only");
        }
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        clientSocket.Connect(serverIP, port);

        Console.WriteLine("Connected to server.");
        SendMessages();

        //byte[] idBuffer = new byte[4];
        //if(clientSocket.Receive(idBuffer) > 0)
        //{
        //    clientId = BitConverter.ToInt32(idBuffer, 0);
        //    Console.WriteLine($"Assigned client ID: {clientId}");

        //    SendMessages();
        //}

    }

    private void SendMessages()
    {
        //int clientId;
        string message;
        while (true)
        {
            message = $"Hello, Client {clientId} Connected.";
            var jsonClientMsg = new { clientId = clientId, message = message };
            string ClientMsg = JsonConvert.SerializeObject(jsonClientMsg);

            Console.Write(ClientMsg);
            //message = Console.ReadLine();

            //if (message.ToLower() == "exit")
            //    break;

            clientSocket.Send(Encoding.ASCII.GetBytes(ClientMsg));
            break;

            //byte[] buffer = new byte[1024];
            //int bytesRead = clientSocket.Receive(buffer);
            //string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            //Console.WriteLine("Received from server: " + response);
        }
    }

    private static void ReceiveHeartbeat()
    {
        byte[] buffer = new byte[BUFFER_SIZE];
        string messageType;
        while (true)
        {
            try
            {
                int bytesRead = clientSocket.Receive(buffer);
                if (bytesRead > 0)
                {
                    string recieveddata = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    dynamic recievedHB = Newtonsoft.Json.JsonConvert.DeserializeObject(recieveddata);

                    if (recievedHB.heartbeat != null)
                    {
                        int heartbeat = recievedHB.heartbeat;
                        Console.WriteLine($"Heartbeat received: {heartbeat}");

                        int ClientHeartbeat = heartbeat + 1;
                        string ack = $"Heartbeat received: {heartbeat} New heartbeat: {ClientHeartbeat}";
                        string msgType = "HB";
                        var jsonClientMsg = new { clientId = clientId, message = ack, messageType = msgType};
                        string ClientMsg = JsonConvert.SerializeObject(jsonClientMsg);

                        Console.Write(ClientMsg);

                        clientSocket.Send(Encoding.ASCII.GetBytes(ClientMsg));

                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection to server lost.");
                break;
            }
        }
    }
    public void Stop()
    {
        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
    }
    public static void Main(string[] args)
    {
        Client client = new Client();
        client.Start("127.0.0.1", 12345); // Connect to localhost (change IP as needed)

        Thread receiveThread = new Thread(ReceiveHeartbeat);
        receiveThread.Start();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        client.Stop();
    }
}
