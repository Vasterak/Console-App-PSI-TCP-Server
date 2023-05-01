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
            AuthComplete,
            AuthFailEnd
        }
        AuthState curAuth, nextAuth;

        //Server messages types
        const string msgMove = "102 MOVE";
        const string msgTurnLeft = "103 TURN LEFT";
        const string msgTurnRight = "104 TURN RIGHT";
        const string msgPickUp = "105 GET MESSAGE";
        const string msgLogout = "106 LOGOUT";
        const string msgKeyRequest = "107 KEY REQUEST";
        const string msgOK = "200 OK";
        private const string msgLoginFailed = "300 LOGIN FAILED";

        //Client information variables
        TcpClient client;
        Protocol protocol;
        string clientName = "";
        int generatedHash = 0, generatedKey = 0, keyID = 0;

        //Client movement variables, index 0 is X, index 1 is Y
        int[] coordFirst = new int[2];
        int[] coordSecond = new int[2];
        int[] coordFirstN = new int[2];
        int[] coordSecondN = new int[2];
        bool initMove = true;

        public Server(TcpClient client)
        {
            this.client = client;
            protocol = new Protocol(client);
        }

        public async Task HandleClientAsync()
        {
            try
            {
                while (client.Connected)
                {
                    await protocol.FlushBuffer();

                    //Server "finite state machine"
                    switch (curComState)
                    {
                        case CommunicationState.Authentication:
                            nextComState = await Authentication();
                            break;

                        case CommunicationState.Navigation:
                            nextComState = await Movement();
                            break;

                        case CommunicationState.Finish:
                            break;

                        case CommunicationState.ServerSyntaxError:
                            break;

                        case CommunicationState.ServerLoginFailed:
                            break;

                    }
                    curComState = nextComState;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
                client.Close();
            }
        }

        private async Task<CommunicationState> Authentication()
        {
            string message;
            //All Auth steps, that are required
            switch (curAuth)
            {
                case AuthState.GetName:
                    Console.WriteLine("-------------------Part1------------------------");
                    //Read input and check validity.
                    message = await protocol.ReceiveAsync();
                    if (await protocol.CheckValidInput(message, 0) == false)
                    {
                        nextAuth = AuthState.AuthFailEnd;
                        break;
                    }

                    //Debug.PrintAsAscii(message);
                    Debug.PrintInValue(message, "Name");


                    clientName = message;
                    nextAuth = AuthState.KeyRequest;
                    
                    break;

                case AuthState.KeyRequest:
                    Console.WriteLine("-------------------Part2------------------------");

                    await protocol.SendAsync(msgKeyRequest);
                    nextAuth = AuthState.KeyID;
                    break;

                case AuthState.KeyID:
                    Console.WriteLine("-------------------Part3------------------------");

                    //Read input and check validity.
                    message = await protocol.ReceiveAsync();
                    if (await protocol.CheckValidInput(message, 1) == false)
                    {
                        nextAuth = AuthState.AuthFailEnd;
                        break;
                    }

                    //Debug.PrintAsAscii(message);
                    Debug.PrintInValue(message, "KeyID");

                    keyID = int.Parse(message);
                    generatedHash = Protocol.CalculateHash(clientName, keyID);
                    generatedKey = Protocol.CalculateConfirmCode(generatedHash, keyID);
                    nextAuth = AuthState.ServerConfirm;
                    break;

                case AuthState.ServerConfirm:
                    Console.WriteLine("-------------------Part4------------------------");

                    Console.WriteLine("GeneratedKey:" + generatedKey);

                    string idk = generatedKey.ToString();
                    await protocol.SendAsync(idk);
                    nextAuth = AuthState.ClientConfirm;
                    break;

                case AuthState.ClientConfirm:
                    Console.WriteLine("-------------------Part5------------------------");

                    //Read input and check validity.
                    message = await protocol.ReceiveAsync();
                    if (await protocol.CheckValidInput(message, 2) == false)
                    {
                        nextAuth = AuthState.AuthFailEnd;
                        break;
                    }
                        
                    Debug.PrintInValue(message, "ClientConfirm");

                    //Check if keys are valid
                    if (Protocol.CalculateHashBack(int.Parse(message), keyID) == generatedHash)
                        nextAuth = AuthState.AuthComplete;
                    else
                    {
                        await protocol.SendAsync(msgLoginFailed);
                        nextAuth = AuthState.AuthFailEnd;

                    }
                    break;

                case AuthState.AuthComplete:
                    await protocol.SendAsync(msgOK);
                    return CommunicationState.Navigation;

                case AuthState.AuthFailEnd:
                    client.Close();
                    return CommunicationState.ServerLoginFailed;
            }
            curAuth = nextAuth;
            return CommunicationState.Authentication;
        }

        private async Task<CommunicationState> Movement()
        {
            string message;
            if (initMove)
            {
                //Sent init move
                await protocol.SendAsync(msgMove);

                //Read input and check valid msg
                message = await protocol.ReceiveAsync();
                if (await protocol.CheckValidInput(message, 3) == false)
                {
                    return CommunicationState.ServerSyntaxError;
                }
                Debug.PrintInValue(message, "First coordinates");




            }
            return CommunicationState.Finish;
        }
    }
}