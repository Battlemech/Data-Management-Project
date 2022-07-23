using System.Threading;
using Main;
using Main.Networking;
using Main.Networking.Client;
using Main.Networking.Messages;
using NUnit.Framework;

namespace Tests
{
    public static class NetworkingTests
    {
        [Test]
        public static void TestSimpleSend()
        {
            string localhost = "127.0.0.1";
            
            MessageServer server = new MessageServer(localhost);
            server.Start();

            MessageClient client = new MessageClient(localhost);
            client.ConnectAsync();
            
            Assert.IsTrue(client.WaitForConnect());

            client.SendMessage(new TestMessage() { Content = "Yes this is very fun!" });
            
            Thread.Sleep(1000);
            
            //todo: receive message of type TestMessage
        }
        
        public class TestMessage : Message
        {
            public string Content;
        }
    }
}