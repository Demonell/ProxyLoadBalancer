using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyLoadBalancer
{
    /// <summary>
    /// Прокси
    /// </summary>
    public class Proxy
    {
        /// <summary>
        /// IP адрес и порт
        /// </summary>
        public IPEndPoint IpEndPoint { get; private set; }

        private readonly List<ServerNode> _serverNodes;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Прокси
        /// </summary>
        /// <param name="ipAddress">IP адрес</param>
        /// <param name="port">Порт</param>
        /// <param name="serverNodes">Серверные ноды</param>
        public Proxy(string ipAddress, int port, List<ServerNode> serverNodes)
        {
            if (serverNodes.Count <= 0)
                throw new ArgumentException("List of server nodes cant be empty", nameof(serverNodes));

            IpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _serverNodes = serverNodes;
        }

        /// <summary>
        /// Начать прослушивание
        /// </summary>
        public void StartListening(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            proxySocket.Bind(IpEndPoint);
            proxySocket.Listen(10000);

            new Thread(() => Listen(proxySocket, _cancellationTokenSource.Token)).Start();
        }

        /// <summary>
        /// Завершить прослушивание
        /// </summary>
        public void StopListening()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Выбрать свободную серверную ноду
        /// </summary>
        public ServerNode FindFreeServerNode()
        {
            return _serverNodes
                .OrderBy(node => node.CurrentConnectionsCount)
                .ThenBy(node => node.TotalConnectionsCount)
                .First();
        }

        private void Listen(Socket proxySocket, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start listening {proxySocket.LocalEndPoint} successfully");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var sender = proxySocket.Accept();
                    var serverNode = FindFreeServerNode();

                    Console.WriteLine($"Forwarding message from {sender.RemoteEndPoint} to {serverNode.IpEndPoint}");

                    var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    receiver.Connect(serverNode.IpEndPoint);
                    serverNode.ConnectionCreated();

                    var requestState = new RequestState(sender, receiver);
                    sender.BeginReceive(requestState.Buffer, 0, RequestState.BufferSize, 0, OnDataReceived, requestState);

                    var responseState = new ResponseState(receiver, sender, serverNode);
                    receiver.BeginReceive(responseState.Buffer, 0, RequestState.BufferSize, 0, OnDataReceived, responseState);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            proxySocket.Close();
        }

        private static void OnDataReceived(IAsyncResult result)
        {
            var state = (RequestState) result.AsyncState;

            try
            {
                var bytesRead = state!.Sender.EndReceive(result);
                if (bytesRead > 0)
                {
                    state.Receiver.Send(state.Buffer, bytesRead, SocketFlags.None);
                    state.Sender.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, OnDataReceived, state);
                }
                else
                {
                    state.Close();
                }
            }
            catch
            {
                state!.Close();
            }
        }

        private class RequestState
        {
            public const int BufferSize = 1024;

            public readonly byte[] Buffer = new byte[BufferSize];

            public Socket Sender { get; }

            public Socket Receiver { get; }

            public RequestState(Socket sender, Socket receiver)
            {
                Sender = sender;
                Receiver = receiver;
            }

            public virtual void Close()
            {
                Receiver.Close();
                Sender.Close();
            }
        }

        private class ResponseState : RequestState
        {
            private readonly ServerNode _serverNode;

            public ResponseState(Socket sender, Socket receiver, ServerNode serverNode) : base(sender, receiver)
            {
                _serverNode = serverNode;
            }

            public override void Close()
            {
                base.Close();
                _serverNode.ConnectionClosed();
            }
        }
    }
}