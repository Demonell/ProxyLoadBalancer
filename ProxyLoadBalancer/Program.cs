using System;
using System.Threading;
using ProxyLoadBalancer.Configuration;

namespace ProxyLoadBalancer
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = BalancerConfiguration.Load("config.json");
            var proxies = configuration.GetProxyList();

            var cts = new CancellationTokenSource();
            foreach (var proxy in proxies)
                proxy.StartListening(cts.Token);

            Console.WriteLine("\nHit any key to stop listening...");
            Console.Read();

            cts.Cancel();
        }
    }
}