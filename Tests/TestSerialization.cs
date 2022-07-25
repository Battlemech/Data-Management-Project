using System;
using System.Diagnostics;
using System.Linq;
using GroBuf;
using GroBuf.DataMembersExtracters;
using Main.Utility;
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
        public static void TestGlobalSerialization()
        {
            //init serializer locally
            Serializer serializer = new Serializer(new AllPropertiesExtractor(), options : GroBufOptions.WriteEmptyObjects);
            
            TestClass original = new TestClass(){Count = 100, Message = "This is a public service announcement. You are dead."};

            //use local serializer
            byte[] localBytes = serializer.Serialize(original);
            TestClass copy = serializer.Deserialize<TestClass>(localBytes);
            
            //test local serializer
            Assert.AreEqual(original.GetType(), copy.GetType());
            Assert.AreEqual(original.Count, copy.Count);
            Assert.AreEqual(original.Message, copy.Message);

            Console.WriteLine("Local serialization succeeded");
            
            //use global serializer
            byte[] globalBytes = Serialization.Serialize(original);
            copy = Serialization.Deserialize<TestClass>(globalBytes);

            //test global serializer
            Assert.AreEqual(original.GetType(), copy.GetType(), "Global serializer failed");
            Assert.AreEqual(original.Count, copy.Count, "Global serializer failed");
            Assert.AreEqual(original.Message, copy.Message, "Global serializer failed");
            
            Console.WriteLine("Global serialization succeeded");
        }

        public class TestClass
        {
            public int Count { get; set; }
            public string Message { get; set; }
        }
        
        public class TestClass2 : TestClass
        {
            public bool IsTrue { get; set; }
        }

        [Test]
        public static void TestTypeSerialization()
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
    }
}