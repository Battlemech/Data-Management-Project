using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Dapper;
using DMP.Utility;
using GroBuf;
using GroBuf.DataMembersExtracters;
using NUnit.Framework;

namespace Tests
{
    public static class TestCustomSerializer
    {
        [Test]
        public static void TestSerializer()
        {
            //init serializer locally //default: AllFieldsExtractor
            Serializer serializer = new Serializer(new AttributeAwareExtractor(), options : GroBufOptions.WriteEmptyObjects);

            byte[] bytes = serializer.Serialize(new TestClass() { One = "1", Two = "2", Three = "3", Four = "4" });
            TestClass copy = serializer.Deserialize<TestClass>(bytes);
            
            Assert.AreEqual("1", copy.One);
            Assert.AreNotEqual("2", copy.Two);
            Assert.AreEqual("3", copy.Three);
            Assert.AreNotEqual("4", copy.Four);
        }

        [Test]
        public static void TestPerformance()
        {
            int itemCount = 100000;
            
            //serializers
            Serializer original = new Serializer(new AllFieldsExtractor(), options : GroBufOptions.WriteEmptyObjects);
            Serializer custom = new Serializer(new AttributeAwareExtractor(), options : GroBufOptions.WriteEmptyObjects);

            //measure default time
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < itemCount; i++)
            {
                //serialize and deserialize class
                TestClass copy = original.Deserialize<TestClass>(original.Serialize(new TestClass(i)));
                
                //make sure serialization was successful
                Assert.AreEqual(i.ToString(), copy.One);
            }
            
            //measure time
            stopwatch.Stop();
            double elapsedOriginal = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            
            //measure custom time
            for (int i = 0; i < itemCount; i++)
            {
                //serialize and deserialize class
                TestClass copy = custom.Deserialize<TestClass>(custom.Serialize(new TestClass(i)));
                
                //make sure serialization was successful
                Assert.AreEqual(i.ToString(), copy.One);
            }
            stopwatch.Stop();
            double elapsedCustom = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"Default serializer: {elapsedOriginal}ms, {elapsedOriginal / itemCount} per item");
            Console.WriteLine($"Custom serializer: {elapsedCustom}ms, {elapsedCustom / itemCount} per item");
        }

        [Test]
        public static void TestCollections()
        {
            List<int> ints = new List<int>() { 1, 2, 3, 4, 5 };
            
            Assert.AreEqual(ints, Serialization.Deserialize<List<int>>(Serialization.Serialize(ints)));

            SortedList<int, string> sortedList = new SortedList<int, string> { { 3, "3" } };

            Assert.AreEqual(sortedList, Serialization.Deserialize<SortedList<int, string>>(Serialization.Serialize(sortedList)));
        }
    }

    public class TestClass
    {
        public string One;
        [PreventSerialization]
        public string Two;
        public string Three;
        public string Four { get; set; }
        
        public TestClass(){}

        public TestClass(int test)
        {
            One = test.ToString();
            Two = test.ToString();
            Three = test.ToString();
            Four = test.ToString();
        }
    }
    
    
}