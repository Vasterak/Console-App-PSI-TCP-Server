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
            IPAddress serverIP = IPAddress.Parse("192.168.10.220");
            const int serverPort = 3999;

            TcpListener server = new TcpListener(serverIP, serverPort);
            server.Start();
            Console.WriteLine($"Server started listening on port {serverPort}");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Console.WriteLine($"\n\n\nNew client connected: {client.Client.RemoteEndPoint}");

                //Set timeout time
                client.ReceiveTimeout = 1000; //in ms

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
            PickSecret,
            ServerSyntaxError,
            ServerLoginFailed,
            SuccessEnd
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
        bool avoidanceManeuver = false;
        int numOfExecutedMoves = 0;
        string message = "";

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
                            nextComState = await Navigation();
                            break;

                        case CommunicationState.PickSecret:
                            nextComState = await PickSecret();
                            break;

                        case CommunicationState.ServerSyntaxError:
                            client.Close();
                            break;

                        case CommunicationState.ServerLoginFailed:
                            client.Close();
                            break;

                        case CommunicationState.SuccessEnd:
                            await protocol.SendAsync(Messages.msgLogout);
                            client.Close();
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
                    message = await protocol.ReceiveAsync(0);
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

                    await protocol.SendAsync(Messages.msgKeyRequest);
                    nextAuth = AuthState.KeyID;
                    break;

                case AuthState.KeyID:
                    Console.WriteLine("-------------------Part3------------------------");

                    //Read input and check validity.
                    message = await protocol.ReceiveAsync(1);
                    if (await protocol.CheckValidInput(message, 1) == false)
                    {
                        nextAuth = AuthState.AuthFailEnd;
                        break;
                    }
                    if (message == Messages.msgRecharing)
                    {
                        nextAuth = AuthState.KeyID;
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
                    message = await protocol.ReceiveAsync(1);
                    if (await protocol.CheckValidInput(message, 2) == false)
                    {
                        nextAuth = AuthState.AuthFailEnd;
                        break;
                    }
                    if (message == Messages.msgRecharing)
                    {
                        nextAuth = AuthState.KeyID;
                        break;
                    }

                    Debug.PrintInValue(message, "ClientConfirm");

                    //Check if keys are valid
                    if (Protocol.CalculateHashBack(int.Parse(message), keyID) == generatedHash)
                        nextAuth = AuthState.AuthComplete;
                    else
                    {
                        await protocol.SendAsync(Messages.msgLoginFailed);
                        nextAuth = AuthState.AuthFailEnd;
                    }
                    break;

                case AuthState.AuthComplete:
                    await protocol.SendAsync(Messages.msgOK);
                    return CommunicationState.Navigation;

                case AuthState.AuthFailEnd:
                    return CommunicationState.ServerLoginFailed;
            }
            curAuth = nextAuth;
            return CommunicationState.Authentication;
        }

        private async Task<CommunicationState> Navigation()
        {
            Console.WriteLine("-------------------Movement------------------------");
            //-----------------------INITIAL MOVEMENT.-------------------- //Recharge is not detected here, fix it
            if (initMove)
            {
                //Sent first move
                await protocol.SendAsync(Messages.msgMove);

                //Read input and check valid msg
                message = await protocol.ReceiveAsync(1);
                if (await protocol.CheckValidInput(message, 3) == false)
                    return CommunicationState.ServerSyntaxError;

                //Save first coord
                coordFirst = protocol.GetCoordinaties(message);

                //Debug.PrintInValue(message, "First coordinates");
                Debug.PrintCoords(coordFirst, 11);

                //If we are on 0,0 we dont need to move
                if (coordFirst[0] == 0 && coordFirst[1] == 0)
                    return CommunicationState.PickSecret;

                //Sent second move
                await protocol.SendAsync(Messages.msgMove);

                //Read input and check valid msg
                message = await protocol.ReceiveAsync(1);
                if (await protocol.CheckValidInput(message, 3) == false)
                    return CommunicationState.ServerSyntaxError;

                //Save second coord
                coordSecond = protocol.GetCoordinaties(message);

                //Debug.PrintInValue(message, "Second coordinates");
                Debug.PrintCoords(coordSecond, 22);

                initMove = false;
            }

            //-----------------------CONTINUOUS MOVEMENT--------------------
            //Very lame solution for stuck situation
            if (coordFirst[0] == coordSecond[0] && coordFirst[1] == coordSecond[1])
            {
                if (coordSecond[0] == 0 || coordSecond[1] == 0)
                {
                    avoidanceManeuver = true;
                }
                else
                {
                    numOfExecutedMoves = 2;
                    await protocol.SendAsync(Messages.msgTurnRight);
                    await protocol.SendAsync(Messages.msgMove);
                    Console.WriteLine("TURN RIGHT FROM AVOIDING ROCK");
                }
            }

            if (avoidanceManeuver)
            {
                avoidanceManeuver = await protocol.ObstacleAvoidance(coordSecond);
                return await ReadNewValues();
            }

            //After second move we are on 0,0
            if (coordSecond[0] == 0 && coordSecond[1] == 0)
                return CommunicationState.PickSecret;

            //Navigating check if 5,7 moved to 5,6 is valid which is, other axis shoud not change
            else if (coordFirstN[1] > coordSecondN[1] && coordFirstN[0] == coordSecondN[0])
            {
                //Check on moving axis if we are on 0
                if (coordSecondN[1] == 0)
                {
                    Console.WriteLine("Robot hit zero on second axis");
                    await protocol.SendAsync(Messages.msgTurnRight);
                    await protocol.SendAsync(Messages.msgMove);
                    numOfExecutedMoves = 2;
                }

                //We are not at the zero, continues until zero is met or we hit the rock
                else
                {
                    await protocol.SendAsync(Messages.msgMove);
                    numOfExecutedMoves = 1;
                }

            }

            else if (coordFirstN[1] == coordSecondN[1] && coordFirstN[0] > coordSecondN[0])
            {
                //Check on moving axis if we are on 0
                if (coordSecondN[0] == 0)
                {
                    Console.WriteLine("We hit zero on first axis");
                    numOfExecutedMoves = 2;
                    await protocol.SendAsync(Messages.msgTurnLeft);
                    await protocol.SendAsync(Messages.msgMove);
                }
                //We are not at the zero, continues until zero is met or we hit the rock
                else
                {
                    await protocol.SendAsync(Messages.msgMove);
                    numOfExecutedMoves = 1;
                }
            }

            else if (coordSecondN[0] > coordFirstN[0] || coordSecondN[1] > coordFirstN[1])
            {
                await protocol.SendAsync(Messages.msgTurnRight);
                await protocol.SendAsync(Messages.msgTurnRight);
                await protocol.SendAsync(Messages.msgMove);
                numOfExecutedMoves = 3;
            }

            coordFirst = coordSecond;

            return await ReadNewValues();
        }

        private async Task<CommunicationState> ReadNewValues()
        {
            //READ NEW VALUES
            for (int i = 0; i < numOfExecutedMoves; i++)
            {
                message = await protocol.ReceiveAsync(1);
                if (await protocol.CheckValidInput(message, 3) == false)
                    return CommunicationState.ServerSyntaxError;

                if (message == Messages.msgRecharing)
                    return CommunicationState.Navigation;

                coordSecond = protocol.GetCoordinaties(message);
            }
            numOfExecutedMoves = 1;

            //Convert to absolute value
            coordFirstN[0] = Math.Abs(coordFirst[0]);
            coordFirstN[1] = Math.Abs(coordFirst[1]);
            coordSecondN[0] = Math.Abs(coordSecond[0]);
            coordSecondN[1] = Math.Abs(coordSecond[1]);

            //Print new received values
            Debug.PrintCoords(coordFirst, 1);
            Debug.PrintCoords(coordSecond, 2);
            return CommunicationState.Navigation;
        }

        private async Task<CommunicationState> PickSecret()
        {
            string message;

            //Read msg
            await protocol.SendAsync(Messages.msgPickUp);

            //Check valid
            message = await protocol.ReceiveAsync(1);
            if (await protocol.CheckValidInput(message, 6) == false)
                return CommunicationState.ServerSyntaxError;

            if (message == Messages.msgRecharing)
                return CommunicationState.Navigation;

            //Print
            Debug.PrintInValue(message, "Secret message");

            //Return end
            return CommunicationState.SuccessEnd;

        }
    }
}