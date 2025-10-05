using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        static object _lock = new object();
        static async Task Main(string[] args)
        {
            try
            {
                string host = Dns.GetHostName();
                IPHostEntry ipHost = Dns.GetHostEntry(host);
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint end = new IPEndPoint(ipAddr, 7777);
                TcpListener listener = new TcpListener(end);

                listener.Start();
                Console.WriteLine("서버가 시작되었습니다.");


                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    clients.Add(client);
                    Console.WriteLine("클라이언트가 입장하였습니다.");
                    Task.Run(() => ClientAsync(client));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"오류 : {e}");

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

        static async Task BroadCast(string message, TcpClient sender)
        {
            List<TcpClient> client;
            lock (_lock)
            {
                client = new List<TcpClient>(clients);
            }

            foreach (var v in client)
            {
                if (sender == v)
                    continue;

                var packet = Packet(message);
                try
                {
                    await v.GetStream().WriteAsync(packet, 0, packet.Length);
                }
                catch
                {
                    Console.WriteLine("오류 발생 : 브로드 캐스트");
                    clients.Remove(v);
                    v.Close();
                }
            }
        }

        static async Task ClientAsync(TcpClient client)
        {
            try
            {
                while (true)
                {

                    NetworkStream stream = client.GetStream();
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

                    BroadCast(message, client);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"클라이언트와의 연결이 끊어졌습니다.");
                client.Close();
                clients.Remove(client);
            }
        }
    }
}
