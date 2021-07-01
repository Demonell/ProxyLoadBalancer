using System.Net;
using System.Threading;

namespace ProxyLoadBalancer
{
    /// <summary>
    /// Серверная нода
    /// </summary>
    public class ServerNode
    {
        /// <summary>
        /// IP адрес и порт
        /// </summary>
        public IPEndPoint IpEndPoint { get; private set; }

        /// <summary>
        /// Текущее кол-во соединений
        /// </summary>
        public long CurrentConnectionsCount => _currentConnectionsCount;

        private long _currentConnectionsCount;

        /// <summary>
        /// Общее кол-во произошедших соединений
        /// </summary>
        public long TotalConnectionsCount => _totalConnectionsCount;

        private long _totalConnectionsCount;

        /// <summary>
        /// Серверная нода
        /// </summary>
        /// <param name="ipAddress">IP адрес</param>
        /// <param name="port">Порт</param>
        public ServerNode(string ipAddress, int port)
        {
            IpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        /// <summary>
        /// Подключение устновлено
        /// </summary>
        public void ConnectionCreated()
        {
            Interlocked.Increment(ref _currentConnectionsCount);
            Interlocked.Increment(ref _totalConnectionsCount);
        }

        /// <summary>
        /// Подключение закрыто
        /// </summary>
        public void ConnectionClosed()
        {
            Interlocked.Decrement(ref _currentConnectionsCount);
        }
    }
}