using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Main;
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
            const int messagesToSend = 100000;
            const int clientCount = 5;
            
            int receivedMessages = 0;
            //start client and server
            MessageServer messageServer = new MessageServer("127.0.0.1");
            messageServer.AddCallback<TestMessage>(((message, connection) =>
            {
                Interlocked.Increment(ref receivedMessages);
            }));
            messageServer.Start();

            List<MessageClient> messageClients = new List<MessageClient>(clientCount);
            for (int i = 0; i < clientCount; i++)
            {
                MessageClient messageClient = new MessageClient("127.0.0.1");
                messageClient.Connect();
                
                messageClients.Add(messageClient);
            }

            Task.Factory.StartNew((() =>
            {
                Stopwatch sendMessages = Stopwatch.StartNew();
                for (int i = 0; i < messagesToSend; i++)
                {
                    foreach (var messageClient in messageClients)
                    {
                        messageClient.SendMessage(new TestMessage());
                    }
                }
                sendMessages.Stop();
                
                Console.WriteLine($"Sent {messagesToSend * clientCount} messages in {sendMessages.ElapsedMilliseconds} ms." +
                                  $"{ (float) sendMessages.ElapsedMilliseconds / (messagesToSend * clientCount)} ms/message");
            }));
            
            Stopwatch receiveMessages = Stopwatch.StartNew();
            
            //saves messages received previously
            TestUtility.AreEqual(messagesToSend * clientCount, (() =>
            {
                Console.WriteLine($"[{receiveMessages.ElapsedMilliseconds} ms]Current received messages: {receivedMessages}");
                return receivedMessages;
            }), "Receive messages", 5000 * clientCount, 100);
            
            receiveMessages.Stop();
            
            Console.WriteLine($"Received {messagesToSend * clientCount} messages in {receiveMessages.ElapsedMilliseconds} ms." +
                              $" { (float) receiveMessages.ElapsedMilliseconds / (messagesToSend * clientCount)} ms/message");
            
            //stop background threads
            messageServer.Stop();
            foreach (var messageClient in messageClients)
            {
                messageClient.Disconnect();
            }
        }
    }
}