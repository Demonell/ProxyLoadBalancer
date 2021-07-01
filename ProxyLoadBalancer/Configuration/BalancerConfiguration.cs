using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProxyLoadBalancer.Configuration
{
    /// <summary>
    /// Конфигурация балансировщика
    /// </summary>
    public class BalancerConfiguration
    {
        /// <summary>
        /// Прокси
        /// </summary>
        public List<ProxyConfig> Proxies { get; set; }

        private BalancerConfiguration()
        {
        }

        /// <summary>
        /// Загрузить конфигурации балансировщика
        /// </summary>
        /// <param name="filePath">Путь к файлу конфигурации</param>
        public static BalancerConfiguration Load(string filePath)
        {
            var configuration = JsonConvert.DeserializeObject<BalancerConfiguration>(File.ReadAllText(filePath));
            return configuration;
        }

        /// <summary>
        /// Получить список прокси
        /// </summary>
        public List<Proxy> GetProxyList()
        {
            return Proxies.Select(proxy => proxy.ToProxy()).ToList();
        }
    }
}
