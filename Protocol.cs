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
        const int FIXEDVALUE = 65536;

        //Incomming message buffers
        private string stringBuffer = "";
        private string msgErrorCode = "234MSGTooLong0649";

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
        //-------------------------------------------------------------
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

            //If msg is longer that allowed just throw error
            if (message.Length > 102)
                return msgErrorCode;

            //Check if message is completed, then retun it
            if (message.Length > 2 && message.Substring(message.Length - 2) == "\a\b")
                return message;

            //Received message is not completed, then wait until is fully received
            else
            {
                //Save part of the msg to buffer and set flag
                stringBuffer += message;

                while (stringBuffer.Length < 102)
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
                        return message;
                    }
                }
                return msgErrorCode;
            }
        }

        public async Task SendAsync(string message)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

            Console.WriteLine($"OUT:{message}:");
        }
    }
}
