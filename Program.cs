using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerV2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");
            const int serverPort = 3999;

            TcpListener server = new TcpListener(serverIP, serverPort);
            server.Start();
            Console.WriteLine($"Server started listening on port {serverPort}");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine($"New client connected: {client.Client.RemoteEndPoint}");

                //Task.Run(() => HandleClientAsync(client));
                Server mainServer = new Server(client);

                _ = mainServer.HandleClientAsync(client);
            }
        }
    }

    class Server
    {
        TcpClient client;
        public Server(TcpClient client)
        {
            this.client = client;
        }

        public async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                //Create instance of protocol class that handles communication
                Protocol protocol = new Protocol(client);

                while (client.Connected)
                {
                    await protocol.FlushBuffer();

                    string message = await protocol.ReceiveAsync();

                    Debug.PrintAsAscii(message);

                    await protocol.SendAsync("107 KEY REQUEST\a\b");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                client.Close();
            }
        }
    }
}