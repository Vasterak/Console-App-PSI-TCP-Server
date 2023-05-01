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

                //Handle client communication
                //Task.Run(() => HandleClientAsync(client));
                Server mainServer = new Server(client);
                _ = mainServer.HandleClientAsync();
            }
        }
    }

    class Server
    {
        enum CommunicationState
        {
            Authentication,
            Navigation,
            Finish,
            ServerSyntaxError,
            ServerKeyOutOfRange,
            ServerLoginFailed
        }
        CommunicationState curComState, nextComState;

        enum AuthState
        {
            GetName,
            KeyRequest,
            KeyID,
            ServerConfirm,
            ClientConfirm,
            AuthComplete
        }
        AuthState curAuth, nextAuth;

        TcpClient client;
        public Server(TcpClient client)
        {
            this.client = client;
        }

        public async Task HandleClientAsync()
        {
            try
            {
                //Create instance of protocol class that handles communication
                Protocol protocol = new Protocol(client);

                while (client.Connected)
                {
                    await protocol.FlushBuffer();

                    switch (curComState)
                    {
                        case CommunicationState.Authentication:

                            nextComState = await Authentication();
                            break;

                        case CommunicationState.Navigation:
                            break;

                        case CommunicationState.Finish:
                            break;

                        case CommunicationState.ServerSyntaxError:
                            break;

                        case CommunicationState.ServerKeyOutOfRange:
                            break;

                        case CommunicationState.ServerLoginFailed:
                            break;

                    }
                    curComState = nextComState;


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

        private async Task<CommunicationState> Authentication()
        {
            switch (curAuth)
            {
                case AuthState.GetName:
                    break;

                case AuthState.KeyRequest:
                    break;

                case AuthState.KeyID:
                    break;

                case AuthState.ServerConfirm: 
                    break;

                case AuthState.ClientConfirm:
                    break;

                case AuthState.AuthComplete:
                    break;
            }
            curAuth = nextAuth;

            return CommunicationState.Authentication;
        }
    }
}