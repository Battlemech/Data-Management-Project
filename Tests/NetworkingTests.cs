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
        
        [Test]
        public static void TestNetworkLoad()
        {
            int messagesToSend = 100000;
            
            int receivedMessages = 0;
            //start client and server
            MessageServer messageServer = new MessageServer("127.0.0.1");
            messageServer.AddCallback<TestMessage>(((message, connection) =>
            {
                Interlocked.Increment(ref receivedMessages);
            }));
            messageServer.Start();

            MessageClient client1 = new MessageClient("127.0.0.1");
            client1.Connect();
            MessageClient client2 = new MessageClient("127.0.0.1");
            client2.Connect();

            for (int i = 0; i < messagesToSend; i++)
            {
                client1.SendMessage(new TestMessage());
                client2.SendMessage(new TestMessage());
            }

            TestUtility.AreEqual(messagesToSend * 2, (() =>
            {
                Console.WriteLine($"Current received messages: {receivedMessages}");
                return receivedMessages;
            }), "Receive messages", messagesToSend, 50);
            
            messageServer.Stop();
            client1.Disconnect();
            client2.Disconnect();
        }
    }
}