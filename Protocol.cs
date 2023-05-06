using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerV2
{
    public class Protocol
    {
        //Connection and stream variables
        private TcpClient client;
        private NetworkStream stream;
        private byte[] receiveBuffer = new byte[1024];

        //Auth variables
        private static readonly int[] serverKey = { 23019, 32037, 18789, 16443, 18189 };
        private static readonly int[] clientKey = { 32037, 29295, 13603, 29533, 21952 };
        private const int keyNumber = 4;
        private const int FIXEDVALUE = 65536;

        //Incomming message buffers
        private const int maxIncommingLength = 98;
        private string stringBuffer = "";


        //Obstacle avoidance variables
        private int maneuverStatIndex = 0;

        public Protocol(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        //------------------------------AUTH------------------------------------------
        public static int CalculateHash(string inputMessage, int index)
        {
            //Sum all chars from input name
            Int16 asciiSum = 0;
            byte[] bytes = Encoding.ASCII.GetBytes(inputMessage);
            foreach (byte b in bytes)
                asciiSum += b;

            return (asciiSum * 1000) % FIXEDVALUE;
        }
        public static int CalculateConfirmCode(int hash, int index)
        {
            int confirmCode = (hash + serverKey[index] % FIXEDVALUE);
            return confirmCode;
        }

        public static int CalculateHashBack(int recivedCode, int index)
        {
            int calcBack = recivedCode - clientKey[index];
            if (calcBack < 0)
                calcBack += FIXEDVALUE;

            return calcBack;
        }

        //---------------------------DATA IN AND OUT----------------------------------
        public async Task FlushBuffer()
        {
            await client.GetStream().FlushAsync();
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
        }

        public async Task<string> ReceiveAsync(int msgID)
        {
            string message = "";
            //Cycle through data in buffer until full msg is found
            for (int i = 0; i < stringBuffer.Length; i++)
            {
                //Check if \a is located at the end of the buffer
                if (stringBuffer[i] == '\a' && stringBuffer.Length == i + 1)
                    break;

                //When first msg is found
                if (stringBuffer[i] == '\a' && stringBuffer[i + 1] == '\b')
                {
                    //Save it to new string
                    string singleMsg = stringBuffer.Substring(0, i);

                    //Remove msg from buffer + remove \a \b, leave rest of msg there
                    stringBuffer = stringBuffer.Remove(0, i+2);
                    return singleMsg;
                }
            }

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));

            //When no valid msg with termination(\a\b) was found in buffer wait for the rest of the msg
            while (stringBuffer.Length < maxIncommingLength)
            {
                //-----------------------TIMEOUT SECTION TEST-START----------------------------------
                //Read new data and check for timeout
                var ReadTask = await Task.WhenAny(stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length), timeoutTask);

                if (ReadTask == timeoutTask)
                    return Messages.msgTimeOutError;
                else
                {
                    int bytesRead = ((Task<int>)ReadTask).Result;
                    message = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    message.TrimEnd('\0');

                    timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));
                    //NOT WORKING CORRECTLY FIX IT! When client is killed This is not working

                    //if (message != "")
                    //{

                    //}
                }
                //-----------------------TIMEOUT SECTION TEST-END-----------------------------------

                //Add again received content to buffer
                stringBuffer += message;

                //Cycle through data in buffer until full msg is found
                for (int i = 0; i < stringBuffer.Length; i++)
                {
                    //Check if \a is located at the end of the buffer
                    if (stringBuffer[i] == '\a' && stringBuffer.Length == i + 1)
                    {
                        if (stringBuffer[i] == '\a' && stringBuffer[i-1] == '\a')
                            return Messages.msgSyntaxError;
                        else
                            break;
                    }
                    else if (msgID == 0 && stringBuffer.Length > 18 && i > 17)
                        return Messages.msgSyntaxError;

                    //When first msg is found
                    else if (stringBuffer[i] == '\a' && stringBuffer[i + 1] == '\b')
                    {
                        //Save it to new string
                        string singleMsg = stringBuffer.Substring(0, i);

                        //Remove msg from buffer + remove \a \b, leave rest of msg there
                        stringBuffer = stringBuffer.Remove(0, i + 2);
                        return singleMsg;
                    }
                }
            }
            return Messages.msgErrorCode;
        }

        public async Task SendAsync(string message)
        {
            Debug.PrintOutValue(message);
            message += '\a';
            message += "\b";
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);            
        }

        //---------------------------DATA VALIDITY CHECK AND ERROR SENDING------------
        public async Task<bool> CheckValidInput(string msg, int msgID)
        {
            if (msg == Messages.msgErrorCode)
            {
                await SendAsync(Messages.msgSyntaxError);
                return false;
            }
            else if (msg == Messages.msgTimeOutError)
            {
                Console.WriteLine("SERVER: Client connection has timed out");
                return false;
            }
            else if (msg == Messages.msgSyntaxError)
            {
                await SendAsync(Messages.msgSyntaxError);
                return false;
            }

            //Check if some trailing and leading spaces were removed
            int getLength = msg.Length;
            msg = msg.Trim();
            if (getLength > msg.Length)
            {
                await SendAsync(Messages.msgSyntaxError);
                return false;
            }

            switch (msgID)
            {
                //CLIENT_USERNAME
                case 0:
                    if (msg.Length > 18 || msg == "RECHARGING")
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_KEY_ID
                case 1:
                    //check if all chars are number, //check if input can be parsed, if not return false
                    if (msg.Length > 3 || IsDigitsOnly(msg) == false || !int.TryParse(msg, out int value))
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    //check if is in valid range
                    if (int.Parse(msg) < 0 || int.Parse(msg) > 4)
                    {
                        await SendAsync(Messages.msgKeyOutOfRange);
                        return false;
                    }
                    break;

                //CLIENT_CONFIRMATION
                case 2:
                    if (msg.Length > 5 || IsDigitsOnly(msg) == false)
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_OK
                case 3:
                    //msg must start with OK
                    if (msg.Length > 10 || !msg.StartsWith("OK", false, null))
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    //msg cannot be lower than this: OK 1 1 and also cannot include , or .
                    if (msg.Length < 6 || msg.Contains('.') || msg.Contains(','))
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }

                    //------------CHECK IF IT CAN BE PARSED-----------
                    //Remove "OK" from message
                    msg = msg.Remove(0, 2);
                    msg = msg.Trim();

                    //split by space
                    string[] numbers = msg.Split(' ');

                    //Check if it can be parsed
                    if (int.TryParse(numbers[0], out int val0) == false || int.TryParse(numbers[1], out int val1) == false)
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }

                    break;

                //CLIENT_RECHARGING
                case 4:
                    if (msg.Length > 10 || msg != "RECHARGING")
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_FULL_POWER
                case 5:
                    if (msg.Length > 10 || msg != "FULL POWER")
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_MESSAGE
                case 6:
                    if (msg.Length > 100)
                    {
                        await SendAsync(Messages.msgSyntaxError);
                        return false;
                    }
                    break;
            }
            return true;
        }
        private static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }


        //--------------------------------MOVEMENT------------------------------------
        public int[] GetCoordinaties(string inputmsg)
        {
            //Remove "OK" from message
            inputmsg = inputmsg.Remove(0, 2);
            inputmsg = inputmsg.Trim();

            //split by space
            string[] numbers = inputmsg.Split(' ');

            //save values
            int[] coordinaties = new int[2];
            coordinaties[0] = int.Parse(numbers[0]);
            coordinaties[1] = int.Parse(numbers[1]);

            return coordinaties;
        }

        public async Task<bool> ObstacleAvoidance(int[] coords)
        {
            Console.WriteLine($"INDEX VALUE: {maneuverStatIndex}");

            switch (maneuverStatIndex)
            {
                case 0:
                    await SendAsync(Messages.msgTurnLeft);
                    maneuverStatIndex++;
                    break;

                case 1:
                    await SendAsync(Messages.msgMove);
                    maneuverStatIndex++;
                    break;

                case 2:

                    if (coords[0] == 0 || coords[1] == 0)
                    {
                        //we reach 0 on one axis
                        maneuverStatIndex = 0;
                        return false;
                    }
                    else
                    {
                        await SendAsync(Messages.msgTurnRight);
                        maneuverStatIndex++;
                    }
                    break;

                case 3:
                        await SendAsync(Messages.msgMove);
                        maneuverStatIndex++;
                    break;

                case 4:
                        await SendAsync(Messages.msgMove);
                        maneuverStatIndex++;
                    break;

                case 5:
                        await SendAsync(Messages.msgTurnRight);
                        maneuverStatIndex++;
                    break;

                case 6:
                        await SendAsync(Messages.msgMove);
                        maneuverStatIndex++;
                    break;

                case 7:
                    await SendAsync(Messages.msgTurnLeft);
                        maneuverStatIndex++;
                        return false;

                case 8:
                    await SendAsync(Messages.msgMove);
                    maneuverStatIndex = 0;
                    return false;

                default:
                    return true;
            }
            return true;
        }
    }
}
