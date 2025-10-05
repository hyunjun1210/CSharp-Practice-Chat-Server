using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint end = new IPEndPoint(ipAddr, 7777);

            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(end);
                    Console.WriteLine("서버에 연결되었습니다.");

                    NetworkStream stream = client.GetStream();

                    _ = Task.Run(() => ReadAsync(stream));
                    SendAsync(stream);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"오류 발생 : {e}");
                }
                finally
                {
                    Console.WriteLine("서버와의 연결을 종료합니다.");
                    client.Close();
                }
            }

            while (true)
            {

            }
        }

        static byte[] Packet(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            byte[] packet = new byte[4 + messageBytes.Length];
            Buffer.BlockCopy(lengthBytes, 0, packet, 0, 4);
            Buffer.BlockCopy(messageBytes, 0, packet, 4, messageBytes.Length);

            return packet;
        }

        static async Task SendAsync(NetworkStream stream)
        {
            while (true)
            {
                string? message = Console.ReadLine();
                var packet = Packet(message);
                await stream.WriteAsync(packet, 0, packet.Length);
            }
        }

        static async Task ReadAsync(NetworkStream stream)
        {
            while (true)
            {
                try
                {
                    byte[] length = new byte[4];
                    int lengthCount = await stream.ReadAsync(length, 0, 4);
                    if (lengthCount < 4)
                        return;
                    int dataLength = BitConverter.ToInt32(length, 0);
                    byte[] data = new byte[dataLength];
                    int totalRead = 0;
                    while (totalRead < dataLength)
                    {
                        int byteCount = await stream.ReadAsync(data, totalRead, dataLength - totalRead);
                        if (byteCount == 0)
                        {
                            return;
                        }

                        totalRead += byteCount;
                    }

                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"받은 메세지 : {message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"오류 발생 : {e}");
                }
            }
            }
    }
}
