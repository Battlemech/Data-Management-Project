using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DMP.Networking;
using DMP.Networking.Messaging;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;
using NUnit.Framework;

namespace Tests
{
    public static class NetworkSerializationTests
    {
        public struct NetworkNotificationMessage
        {
            public string Notification;
        }
        
        [Test]
        public static void TestNetworkSerialization()
        {
            NetworkNotificationMessage message = new NetworkNotificationMessage()
                { Notification = "That is what happens when an unstoppable force meets an immovable object" };

            NetworkSerializer serializer = new NetworkSerializer();
            
            for (int i = 0; i < 10000; i++)
            {
                //"receive" invalid data for testing purposes
                serializer.Deserialize(Encoding.ASCII.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture)));
                
                //message to bytes
                byte[] messageAsBytes = Serialization.Serialize(message);
                Console.WriteLine($"MessageLength: {messageAsBytes.Length}");
                
                //bytes to network bytes
                byte[] messageAsNetworkBytes = NetworkSerializer.Serialize(messageAsBytes);
                Console.WriteLine($"Total bytes: {messageAsNetworkBytes.Length}");
                
                //network bytes to bytes
                List<byte[]> deserializedMessages = serializer.Deserialize(messageAsNetworkBytes);
                Assert.AreEqual(deserializedMessages.Count, 1);
                byte[] deserializedMessageBytes = deserializedMessages[0];
                
                Assert.AreEqual(messageAsBytes, deserializedMessageBytes);
                
                //bytes to message
                NetworkNotificationMessage deserializedMessage =
                    Serialization.Deserialize<NetworkNotificationMessage>(deserializedMessageBytes);

                Assert.AreEqual(message.Notification, deserializedMessage.Notification);
            }
        }

        [Test]
        public static void TestPartialSendSerialization()
        {
            NetworkNotificationMessage notificationMessage = new NetworkNotificationMessage()
                { Notification = "I love you 3000" };

            int startArrayCount = 10;

            //setup message parts
            byte[] messageBytes = NetworkSerializer.Serialize(Serialization.Serialize(notificationMessage));
            Assert.IsTrue(startArrayCount < messageBytes.Length);
            Console.WriteLine($"StartArrayLength: {startArrayCount}, RemainingArrayLength: {messageBytes.Length - startArrayCount}");
            byte[] messageStart = new byte[startArrayCount];
            Array.Copy(messageBytes, 0, messageStart, 0, startArrayCount);
            byte[] messageEnd = new byte[messageBytes.Length - startArrayCount];
            Array.Copy(messageBytes, startArrayCount, messageEnd, 0, messageBytes.Length - startArrayCount);
            
            Assert.AreEqual(messageBytes.Length, messageStart.Length + messageEnd.Length);

            NetworkSerializer serializer = new NetworkSerializer();
            
            //simulate receiving of partial messages
            Assert.AreEqual(0, serializer.Deserialize(messageStart).Count);
            List<byte[]> messages = serializer.Deserialize(messageEnd);
            Assert.AreEqual(1, messages.Count);

            Assert.AreEqual(notificationMessage.Notification, Serialization.Deserialize<NetworkNotificationMessage>(messages[0]).Notification);

            //simulate receiving of multiple messages
            messages.Clear();
            int receiveCount = 5;
            for (int i = 0; i < receiveCount; i++)
            {
                messages.AddRange(serializer.Deserialize(messageBytes));
            }
            
            //receive partial next message
            messages.AddRange(serializer.Deserialize(messageStart));
            
            Assert.AreEqual(receiveCount, messages.Count);
            for (int i = 0; i < receiveCount; i++)
            {
                Assert.AreEqual(notificationMessage.Notification, Serialization.Deserialize<NetworkNotificationMessage>(messages[i]).Notification);
            }
        }

        [Test]
        public static void DeserializeEmptyByteArray()
        {
            NetworkSerializer serializer = new NetworkSerializer();

            Assert.AreEqual(0, serializer.Deserialize(new byte[] {}).Count);
        }

        [Test]
        public static void DeserializeRandomByteArray()
        {
            NetworkSerializer serializer = new NetworkSerializer();
            
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
            Assert.AreEqual(0, serializer.Deserialize(new byte[] {0,1,2,3,4,5,6}).Count);
        }

        [Test]
        public static void DeserializeTestMessage()
        {
            Type type = typeof(SetValueMessage);
            SetValueMessage message = new SetValueMessage("123", "456", new byte[1] { 9 }, type.AssemblyQualifiedName, 0);
            NetworkSerializer serializer = new NetworkSerializer();

            //make sure received bytes equal sent bytes
            byte[] toSend = message.Serialize();
            Console.WriteLine($"Sending {toSend.Length} bytes!");
            
            byte[] receivedMessage = serializer.Deserialize(toSend)[0];
            Assert.AreEqual(Serialization.Serialize(message), receivedMessage, "Valid byte serialization");
            Console.WriteLine("Received valid bytes");
            Console.WriteLine(message.Type);
            
            //make sure message was deserialized correctly
            SetValueMessage copy = Serialization.Deserialize<SetValueMessage>(receivedMessage);
            
            Assert.AreEqual(message.DatabaseId, copy.DatabaseId);
            Assert.AreEqual(message.ValueId, copy.ValueId);
            Assert.AreEqual(message.ModCount, copy.ModCount);
            Assert.AreEqual(message.Value, copy.Value);
            Assert.AreEqual(type, Type.GetType(message.Type, true));
        }
    }
}