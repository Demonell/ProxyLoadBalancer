namespace ProxyLoadBalancer.Configuration
{
    /// <summary>
    /// Конфигурация серверного нода
    /// </summary>
    public class ServerNodeConfig
    {
        /// <summary>
        /// IP адрес
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Порт
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Преобразовать в серверную ноду
        /// </summary>
        public ServerNode ToServerNode()
        {
            return new(IpAddress, Port);
        }
    }
}
