using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProxyLoadBalancer.Configuration;

namespace ProxyLoadBalancer
{
    class Program
    {
        private const int localProt = 6482;
        private const string localIp = "127.0.0.1";
        private const int TargetPort = 4830;
        private const string TargetIp = "127.0.0.1";

        static async Task Main(string[] args)
        {
            var configuration = BalancerConfiguration.Load("config.json");
            var proxies = configuration.GetProxyList();

            var cts = new CancellationTokenSource();
            foreach (var proxy in proxies)
                proxy.StartListening(cts.Token);

            //_ = RunTcpListenerAsync(cts.Token);
            //_ = RunSocketAsync(cts.Token);

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();

            cts.Cancel();
        }

        public static async Task RunSocketAsync(CancellationToken cancellationToken)
        {
            //Server IP address  
            var ip = IPAddress.Parse(localIp);
            var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(ip, localProt));
            serverSocket.Listen(10000);
            Console.WriteLine($"Start listening {serverSocket.LocalEndPoint} successfully");
            var myThread = new Thread(Listen);
            myThread.Start(serverSocket);
        }

        public class thSock
        {
            public Socket tcp1 { get; set; }
            public Socket tcp2 { get; set; }
        }

        //Monitor client connection
        private static void Listen(object obj)
        {
            var serverSocket = (Socket)obj;
            var ip = IPAddress.Parse(TargetIp);
            while (true)
            {
                var tcp1 = serverSocket.Accept();
                var tcp2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcp2.Connect(new IPEndPoint(ip, TargetPort));
                //The target host returns data
                ThreadPool.QueueUserWorkItem(new WaitCallback(SwapMsg), new thSock
                {
                    tcp1 = tcp2,
                    tcp2 = tcp1
                });
                //Intermediate host requests data
                ThreadPool.QueueUserWorkItem(new WaitCallback(SwapMsg), new thSock
                {
                    tcp1 = tcp1,
                    tcp2 = tcp2
                });
            }
        }
        ///Two tcp connections to exchange data, one sending and one receiving
        public static void SwapMsg(object obj)
        {
            var mSocket = (thSock)obj;
            while (true)
            {
                try
                {
                    var result = new byte[1024];
                    var num = mSocket.tcp2.Receive(result, result.Length, SocketFlags.None);
                    if (num == 0) //Accept the empty packet and close the connection
                    {
                        if (mSocket.tcp1.Connected)
                        {
                            mSocket.tcp1.Close();
                        }
                        if (mSocket.tcp2.Connected)
                        {
                            mSocket.tcp2.Close();
                        }
                        break;
                    }
                    mSocket.tcp1.Send(result, num, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (mSocket.tcp1.Connected)
                    {
                        mSocket.tcp1.Close();
                    }
                    if (mSocket.tcp2.Connected)
                    {
                        mSocket.tcp2.Close();
                    }
                    break;
                }
            }
        }

        public static async Task RunTcpListenerAsync(CancellationToken cancellationToken)
        {
            var address = IPAddress.Parse("127.0.0.1");
            var listener = new TcpListener(address, 6482);
            listener.Start();
            cancellationToken.Register(listener.Stop);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var client = await listener.AcceptTcpClientAsync())
                    {
                        var stream = client.GetStream();
                        var bytes = new byte[256];
                        string data;
                        int i;

                        // Loop to receive all the data sent by the client.
                        //while ((i = await stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken)) != 0)
                        //{
                        //    // Translate data bytes to a ASCII string.
                        //    data = Encoding.ASCII.GetString(bytes, 0, i);
                        //    Console.WriteLine("Received: {0}", data);

                        //    // Process the data sent by the client.
                        //    //data = data.ToUpper();

                        //    //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                        //    //// Send back a response.
                        //    //stream.Write(msg, 0, msg.Length);
                        //    //Console.WriteLine("Sent: {0}", data);
                        //}

                        //return;


                        var allBytes = new List<byte>();
                        i = await stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken);

                        // Loop to receive all the data sent by the client.
                        while (i != 0)
                        {
                            allBytes.AddRange(bytes);

                            bytes = new byte[256];
                            i = stream.DataAvailable
                                ? await stream.ReadAsync(bytes, 0, bytes.Length, cancellationToken)
                                : 0;
                        }

                        if (allBytes.Count > 0)
                        {
                            Console.WriteLine($"Received from client: {allBytes.Count}");

                            var received = SendReceiveRemoteServer("127.0.0.1", 4830, allBytes.ToArray());

                            // Send back a response.
                            await stream.WriteAsync(received, 0, received.Length, cancellationToken);
                            //Console.WriteLine($"Sent to client: {received.Length}");
                        }
                    }
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("TcpListener stopped listening because cancellation was requested.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling client: {ex.Message}{Environment.NewLine}{ex}");
                }
            }
        }

        private static byte[] SendReceiveRemoteServer(string host, int port, byte[] data)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer 
                // connected to the same address as specified by the server, port
                // combination.
                var client = new TcpClient(host, port);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                var stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                //Console.WriteLine($"Sent to server: {data.Length}");

                // Receive the TcpServer.response.

                // Read the first batch of the TcpServer response bytes.
                var bytes = new byte[256];
                var allBytes = new List<byte>();
                var i = stream.Read(bytes, 0, bytes.Length);

                // Loop to receive all the data sent by the client.
                while (i != 0)
                {
                    allBytes.AddRange(bytes);

                    bytes = new byte[256];
                    i = stream.DataAvailable
                        ? stream.Read(bytes, 0, bytes.Length)
                        : 0;
                }

                //Console.WriteLine($"Received from server: {data.Length}");

                // Close everything.
                stream.Close();
                client.Close();

                return allBytes.ToArray();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            return new byte[0];
        }
    }
}