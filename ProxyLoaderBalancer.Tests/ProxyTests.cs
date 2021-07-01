using System;
using System.Collections.Generic;
using ProxyLoadBalancer;
using Xunit;

namespace ProxyLoaderBalancer.Tests
{
    public class ProxyTests
    {
        [Fact]
        public void FindFreeServerNode_BusyFirstNode_SelectSecond()
        {
            var busyNode = new ServerNode("127.0.0.1", 1);
            var freeNode = new ServerNode("127.0.0.1", 2);

            var proxy = new Proxy("127.0.0.1", 0, new List<ServerNode>
            {
                busyNode,
                freeNode
            });

            busyNode.ConnectionCreated();

            var node = proxy.FindFreeServerNode();

            Assert.Equal(freeNode, node);
        }

        [Fact]
        public void FindFreeServerNode_BusyFirstNode_FreeFirstNode_SelectSecond()
        {
            var usedNode = new ServerNode("127.0.0.1", 1);
            var notUsedNode = new ServerNode("127.0.0.1", 2);

            var proxy = new Proxy("127.0.0.1", 0, new List<ServerNode>
            {
                usedNode,
                notUsedNode
            });

            usedNode.ConnectionCreated();
            usedNode.ConnectionClosed();

            var node = proxy.FindFreeServerNode();

            Assert.Equal(notUsedNode, node);
        }
    }
}