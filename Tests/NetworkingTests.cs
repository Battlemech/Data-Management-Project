using System;
using System.Threading;
using Main;
using Main.Networking;
using Main.Networking.Client;
using Main.Networking.Messages;
using Main.Networking.Server;
using NUnit.Framework;

namespace Tests
{
    public static class NetworkingTests
    {
        [Test]
        public static void TestSimpleSend()
        {
            string localhost = "127.0.0.1";
            string messageContent = "Yes this is very fun!";
            
            MessageServer server = new MessageServer(localhost);
            server.Start();

            MessageClient client = new MessageClient(localhost);
            client.ConnectAsync();

            Assert.IsTrue(client.WaitForConnect());
            
            //wait for message to be received: Server
            ManualResetEvent receivedMessage = new ManualResetEvent(false);
            server.AddCallback<TestMessage>((data, session) =>
            {
                //make sure the received message is correct
                Assert.AreEqual(messageContent, data.Content);

                receivedMessage.Set();
            });
            client.SendMessage(new TestMessage() { Content = messageContent });
            Assert.IsTrue(receivedMessage.WaitOne(Options.DefaultTimeout), "Received message: Server");
            
            receivedMessage.Reset();
            
            //wait for message to be received: Client
            client.AddCallback<TestMessage>((value =>
            {
                //make sure the received message is correct
                Assert.AreEqual(messageContent, value.Content);

                receivedMessage.Set();
            }));
            server.Broadcast(new TestMessage() { Content = messageContent });
            Assert.IsTrue(receivedMessage.WaitOne(Options.DefaultTimeout), "Received message: Client");
            
            //remove callbacks
            Assert.AreEqual(1, server.RemoveCallbacks<TestMessage>());
            Assert.AreEqual(1, client.RemoveCallbacks<TestMessage>());
        }
        
        public class TestMessage : Message
        {
            public string Content { get; set; }
            public string Test;

            public TestMessage()
            {
            
            }
        }
    }
}