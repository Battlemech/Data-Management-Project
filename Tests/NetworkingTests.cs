using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Main;
using Main.Networking.Messaging;
using Main.Networking.Messaging.Client;
using Main.Networking.Messaging.Server;
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
            const int messagesToSend = 10000;
            const int clientCount = 10;
            
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
                Assert.IsTrue(messageClient.ConnectAsync());
                
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
                messageClient.DisconnectAsync();
            }
        }

        public class TestRequest : RequestMessage<TestReply>
        {
            public int PleaseTransform;
        }
        
        public class TestReply : ReplyMessage
        {
            public string Transformed;
            public TestReply(RequestMessage requestMessage) : base(requestMessage)
            {
            }
        }
        
        [Test]
        public static void TestRequestReply()
        {
            string localhost = "127.0.0.1";
            int toTransform = 100;

            MessageServer server = new MessageServer(localhost);
            server.Start();

            MessageClient client = new MessageClient(localhost);
            client.ConnectAsync();

            Assert.IsTrue(client.WaitForConnect());

            TestRequest request = new TestRequest() { PleaseTransform = toTransform };
            
            //add request callback to server
            server.AddCallback<TestRequest>(((message, session) =>
            {
                //transform int
                var testReply = new TestReply(message) { Transformed = message.PleaseTransform.ToString() };

                //send reply
                session.SendMessage(testReply);
            }));

            Assert.IsTrue(client.SendRequest(request, out TestReply reply), "Received reply");
            Assert.AreEqual(request.Id, reply.Id, "Id persistence");
            Assert.AreEqual(toTransform.ToString(), reply.Transformed, "server value transformation");

            //remove callback from server
            Assert.AreEqual(1, server.RemoveCallbacks<TestRequest>());
            
            Assert.IsFalse(client.SendRequest(request, out reply, 1000), "Received reply, but callback was removed");
            
            server.Stop();
            client.DisconnectAsync();
        }

        [Test]
        public static void TestBroadcastToOthers()
        {
            string localhost = "127.0.0.1";
            MessageServer server = new MessageServer(localhost);
            server.Start();

            MessageClient originClient = new MessageClient(localhost);
            originClient.ConnectAsync();

            MessageClient peerClient1 = new MessageClient(localhost);
            peerClient1.ConnectAsync();
            
            MessageClient peerClient2 = new MessageClient(localhost);
            peerClient2.ConnectAsync();
            
            Assert.IsTrue(originClient.WaitForConnect());
            Assert.IsTrue(peerClient1.WaitForConnect());
            Assert.IsTrue(peerClient2.WaitForConnect());

            ManualResetEvent originReceived = new ManualResetEvent(false);
            ManualResetEvent peer1Received = new ManualResetEvent(false);
            ManualResetEvent peer2Received = new ManualResetEvent(false);
            
            originClient.AddCallback<TestMessage>((value =>
            {
                Console.WriteLine("Origin received message");
                originReceived.Set();
            }));
            peerClient1.AddCallback<TestMessage>((value =>
            {
                Console.WriteLine("Peer 1 received message");
                peer1Received.Set();
            }));
            peerClient2.AddCallback<TestMessage>((value =>
            {
                Console.WriteLine("Peer 2 received message");
                peer2Received.Set();
            }));
            server.AddCallback<TestMessage>(((message, session) =>
            {
                Console.WriteLine("Server is broadcasting message to others");
                Assert.IsTrue(server.BroadcastToOthers(message, session));
            }));
            
            //send message from client
            originClient.SendMessage(new TestMessage());
            
            //wait for reply
            Assert.IsTrue(peer1Received.WaitOne(1000));
            Assert.IsTrue(peer2Received.WaitOne(1000));
            
            //wait for reply, expecting none to come
            Assert.IsFalse(originReceived.WaitOne(1000));
        }
    }
}