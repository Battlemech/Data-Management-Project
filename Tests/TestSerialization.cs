using System;
using GroBuf;
using GroBuf.DataMembersExtracters;
using Main;
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
            
            //compare bytes
            Assert.AreEqual(localBytes, globalBytes);
            
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
    }
}