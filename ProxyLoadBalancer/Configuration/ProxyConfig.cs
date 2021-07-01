using System.Collections.Generic;
using System.Linq;

namespace ProxyLoadBalancer.Configuration
{
    /// <summary>
    /// Конфигурация прокси
    /// </summary>
    public class ProxyConfig
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
        /// Серверный ноды
        /// </summary>
        public List<ServerNodeConfig> ServerNodes { get; set; }

        /// <summary>
        /// Преобразовать в прокси
        /// </summary>
        public Proxy ToProxy()
        {
            return new(IpAddress, Port, ServerNodes.Select(node => node.ToServerNode()).ToList());
        }
    }
}
