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
        const int FIXEDVALUE = 65536;

        //Incomming message buffers
        const int maxIncommingLength = 102;
        private string stringBuffer = "";

        //Error messages
        private const string msgErrorCode = "234MSGTooLong0649";
        private const string msgLoginFailed = "300 LOGIN FAILED";
        private const string msgSyntaxError = "301 SYNTAX ERROR";
        private const string msgLogicError = "302 LOGIC ERROR";
        private const string msgKeyOutOfRange = "303 KEY OUT OF RANGE";

        public Protocol(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        //------------------------------AUTH----------------------------
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

        public async Task<string> ReceiveAsync()
        {
            string message;
            int bytesRead;

            //Initial read
            bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            message = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
            message.TrimEnd('\0');

            //If msg is longer than any allowed just throw error
            if (message.Length > 102)
                return msgErrorCode;

            //Check if message is completed, then retun it
            if (message.Length > 2 && message.Substring(message.Length - 2) == "\a\b")
                return message.Remove(message.Length - 2);

            //Received message is not completed, then wait until is fully received
            else
            {
                //Save part of the msg to buffer and set flag
                stringBuffer += message;

                while (stringBuffer.Length < maxIncommingLength)
                {
                    //Read again new data
                    bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    message = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    message.TrimEnd('\0');

                    stringBuffer += message;

                    //Check if msg end was received
                    if (stringBuffer.Substring(stringBuffer.Length - 2) == "\a\b")
                    {
                        message = stringBuffer;
                        stringBuffer = "";
                        return message.Remove(message.Length - 2);
                    }
                }
                return msgErrorCode;
            }
        }

        public async Task SendAsync(string message)
        {
            Debug.PrintOutValue(message);
            message += '\a';
            message += "\b";
            byte[] sendBuffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);            
        }

        //---------------------------DATA VALIDITY CHECK AND ERROR SENDING----------------------------------
        public async Task<bool> CheckValidInput(string msg, int msgID)
        {
            if (msg == msgErrorCode)
            {
                await SendAsync(msgSyntaxError);
                return false;
            }

            switch (msgID)
            {
                //CLIENT_USERNAME
                case 0:
                    if (msg.Length > 18 || msg == "RECHARGING")
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_KEY_ID
                case 1:
                    //check if all chars are number, //check if input can be parsed, if not return false
                    if (msg.Length > 3 || IsDigitsOnly(msg) == false || !int.TryParse(msg, out int value))
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }
                    //check if is in valid range
                    if (int.Parse(msg) < 0 && int.Parse(msg) > 4)
                    {
                        await SendAsync(msgKeyOutOfRange);
                        return false;
                    }
                    break;

                //CLIENT_CONFIRMATION
                case 2:
                    if (msg.Length > 5 || IsDigitsOnly(msg) == false)
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_OK
                case 3:
                    //msg must start with OK
                    if (msg.Length > 10 || !msg.StartsWith("OK", false, null))
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }

                    break;

                //CLIENT_RECHARGING
                case 4:
                    if (msg.Length > 10 || msg != "RECHARGING")
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_FULL_POWER
                case 5:
                    if (msg.Length > 10 || msg != "FULL POWER")
                    {
                        await SendAsync(msgSyntaxError);
                        return false;
                    }
                    break;

                //CLIENT_MESSAGE
                case 6:
                    if (msg.Length > 100)
                    {
                        await SendAsync(msgSyntaxError);
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

    }
}
