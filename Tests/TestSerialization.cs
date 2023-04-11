using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DMP;
using DMP.Networking.Messaging;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Objects;
using DMP.Threading;
using DMP.Utility;
using GroBuf;
using GroBuf.DataMembersExtracters;
using NUnit.Framework;

namespace Tests
{
    public static class TestSerialization
    {
        [Test]
        public static void TestStringSerialization()
        {
            string test = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor " +
                          "invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam " +
                          "et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est " +
                          "Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed" +
                          " diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam" +
                          " voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd" +
                          " gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
            
            
            Assert.AreEqual(test, Serialization.Deserialize<string>(Serialization.Serialize(test)));
        }

        [Test]
        public static void TestMessageSerialization()
        {
            SetValueMessage message = new SetValueMessage("123", "123213", 123123, new byte[2]{1, 2});
            SetValueMessage copy = Serialization.Deserialize<SetValueMessage>(Serialization.Serialize(message));
         
            Assert.AreEqual(message.DatabaseId, copy.DatabaseId);
            Assert.AreEqual(message.ValueId, copy.ValueId);
            Assert.AreEqual(message.ModCount, copy.ModCount);
            Assert.AreEqual(message.Value, copy.Value);

            TestRequest message2 = new TestRequest() { Test = 123213 };
            TestRequest copy2 = Serialization.Deserialize<TestRequest>(Serialization.Serialize(message2));
            Assert.AreEqual(message2.Test, copy2.Test);
        }

        private class TestRequest : RequestMessage<TestReply>
        {
            public int Test { get; init; }
        }

        private class TestReply : ReplyMessage
        {
            public TestReply(RequestMessage requestMessage) : base(requestMessage)
            {
            }
        }
        
        [Test]
        public static void TestGlobalSerialization()
        {
            //init serializer locally //default: AllFieldsExtractor
            Serializer serializer = new Serializer(new AttributeAwareExtractor(), options : GroBufOptions.WriteEmptyObjects);
            
            TestClass original = new TestClass(){Count = 100, Message = "This is a public service announcement. You are dead.", DontSerializeThis = 12, DontSerializeThis2 = 24};

            //use local serializer
            byte[] localBytes = serializer.Serialize(original);
            TestClass copy = serializer.Deserialize<TestClass>(localBytes);
            
            //test local serializer
            Assert.AreEqual(original.GetType(), copy.GetType());
            Assert.AreEqual(original.Count, copy.Count);
            Assert.AreEqual(original.Message, copy.Message);
            Assert.AreNotEqual(original.DontSerializeThis, copy.DontSerializeThis);
            Assert.AreNotEqual(original.DontSerializeThis2, copy.DontSerializeThis2);

            Console.WriteLine("Local serialization succeeded");
            
            //use global serializer
            byte[] globalBytes = Serialization.Serialize(original);
            copy = Serialization.Deserialize<TestClass>(globalBytes);

            //test global serializer
            Assert.AreEqual(original.GetType(), copy.GetType(), "Global serializer failed");
            Assert.AreEqual(original.Count, copy.Count, "Global serializer failed");
            Assert.AreEqual(original.Message, copy.Message, "Global serializer failed");
            Assert.AreNotEqual(original.DontSerializeThis, copy.DontSerializeThis);
            Assert.AreNotEqual(original.DontSerializeThis2, copy.DontSerializeThis2);
            
            Console.WriteLine("Global serialization succeeded");
        }

        public class TestClass
        {
            public int Count { get; set; }
            public string Message;
            
            public double DontSerializeThis { get; set; }

            [PreventSerialization]
            public double DontSerializeThis2;
        }
        
        public class TestClass2 : TestClass
        {
            public bool IsTrue { get; set; }
        }

        [Test]
        public static void TestImplicitTypeSerialization()
        {
            Stopwatch serializationTime = new Stopwatch();
            Stopwatch deserializationTime = new Stopwatch();

            Type original = typeof(TestClass);

            //serialize type
            serializationTime.Start();
            string typeName = original.FullName;
            byte[] bytes = Serialization.Serialize(typeName);
            serializationTime.Stop();
            
            //deserialize type
            deserializationTime.Start();
            string deserializedName = Serialization.Deserialize<string>(bytes);
            Type copy = Type.GetType(deserializedName);
            deserializationTime.Stop();
            
            //make sure names equal
            Assert.AreEqual(typeName, deserializedName);
            Assert.AreEqual(original, copy);

            Console.WriteLine($"Deserialized type: {deserializedName}. Serialized: {serializationTime.ElapsedMilliseconds} ms. Deserialized: {deserializationTime.ElapsedMilliseconds} ms");
        }

        [Test]
        public static void TestSerializationPerformance()
        {
            string text = "Tests.TestSerialization+TestClass";
            
            int count = 10000;
            long[] serializationTime = new long[count];
            long[] deserializationTime = new long[count];

            Stopwatch measurer = new Stopwatch();

            for (int i = 0; i < count; i++)
            {
                //measure serialization
                measurer.Restart();
                byte[] bytes = Serialization.Serialize(text);
                measurer.Stop();
                serializationTime[i] = measurer.ElapsedMilliseconds;

                //measure deserialization
                measurer.Restart();
                string copy = Serialization.Deserialize<string>(bytes);
                measurer.Stop();
                deserializationTime[i] = measurer.ElapsedMilliseconds;
                
                Assert.AreEqual(text, copy);
            }

            long totalSerializationTime = serializationTime.Sum();
            long totalDeserializationTime = deserializationTime.Sum();
            long totalTime = totalDeserializationTime + totalSerializationTime;
            
            Console.WriteLine($"(de)serialized {count} items in {totalTime} ms ({totalTime / count} ms/string)");
            Console.WriteLine($"Average serialization time: {serializationTime.Average()} ms");
            Console.WriteLine($"Average deserialization time: {deserializationTime.Average()} ms");
        }

        [Test]
        public static void TestSuperClassSerialization()
        {
            //create test object
            TestClass2 testClass2 = new TestClass2() { Count = 10, IsTrue = false, Message = "Yeah!" };

            //copy super object
            byte[] localBytes = Serialization.Serialize(testClass2);
            TestClass2 copy = Serialization.Deserialize<TestClass2>(localBytes);
            
            Assert.AreEqual(testClass2.IsTrue, copy.IsTrue);
            Assert.AreEqual(testClass2.Count, copy.Count);
            Assert.AreEqual(testClass2.Message, copy.Message);
            
            //try accessing data from lower object
            TestClass derivedCopy = Serialization.Deserialize<TestClass>(localBytes);
            Assert.AreEqual(testClass2.Count, derivedCopy.Count);
            Assert.AreEqual(testClass2.Message, derivedCopy.Message);
        }

        [Test]
        public static void TestThreadedPerformance()
        {
            int threadCount = 100;
            int listCount = 10000;
            
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Task[] tasks = new Task[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run((() =>
                {
                    Assert.IsTrue(resetEvent.WaitOne(3000));
                    
                    List<int> list = new List<int>();
                    for (int j = 0; j < listCount; j++)
                    {
                        list.Add(j);
                        Assert.AreEqual(j + 1, Serialization.Deserialize<List<int>>(Serialization.Serialize(list)).Count);
                    }
                }));
            }

            resetEvent.Set();
            Task.WaitAll(tasks, 10000);
        }

        [Test]
        public static void TestSynchronisedObjectDeserialization()
        {
            SynchronisedClient client = new SynchronisedClient();

            TestObject o = new TestObject("Yeah");
            //make sure constructor() was called by class constructor
            Assert.IsTrue(o.ConstructorCalled);
            
            byte[] bytes = Serialization.Serialize(o);
            TestObject copy = Serialization.Deserialize<TestObject>(bytes);

            //make sure constructor() was called by deserialization callback
            Assert.IsTrue(copy.ConstructorCalled);
        }

        private class TestObject : SynchronisedObject
        {
            public bool ConstructorCalled = false;
            public TestObject(string id, bool isPersistent = false) : base(id, isPersistent)
            {
                
            }

            protected override void Constructor()
            {
                Console.WriteLine("Invoked constructor");
                ConstructorCalled = true;
            }
        }

        [Test]
        public static void TestByteLength()
        {
            string test = "Lorem ipsum et doloret et cetera et cetera et cetera";
            Console.WriteLine($"Default serializer byte size: {new Serializer(new AllPropertiesExtractor()).Serialize(test).Length}");
            Console.WriteLine($"Custom serializer byte size: {new Serializer(new AttributeAwareExtractor()).Serialize(test).Length}");
        }
        
        [Test]
        public static void TestExplicitTypeSerialization()
        {
            Type type = typeof(ConcurrentScheduler);
            byte[] bytes = Serialization.Serialize(type);
            Type deserialized = Serialization.Deserialize<Type>(bytes);
            
            Assert.AreEqual(type, deserialized);
        }
    }
}