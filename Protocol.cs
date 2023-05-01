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
        private TcpClient client;
        private NetworkStream stream;
        private byte[] receiveBuffer = new byte[1024];

        public Protocol(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        public async Task FlushBuffer()
        {
            await client.GetStream().FlushAsync();
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
        }

        public async Task<string> ReceiveAsync()
        {
            int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            string message = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);

            return message.TrimEnd('\0');
        }

        public async Task SendAsync(string message)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);

            Console.WriteLine($"Sent msg:{message}:");
        }
    }
}
