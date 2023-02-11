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
            Assert.AreEqual("4", copy.Four);
        }

        [Test]
        public static void TestPerformance()
        {
            int itemCount = 100000;
            
            //serializers
            Serializer original = new Serializer(new AllFieldsExtractor(), options : GroBufOptions.WriteEmptyObjects);
            Serializer custom = new Serializer(new AttributeAwareExtractor(), options : GroBufOptions.WriteEmptyObjects);

            //serialization time
            double elapsedOriginal;
            double elapsedCustom;
            
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
            elapsedOriginal = stopwatch.ElapsedMilliseconds;
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
            elapsedCustom = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"Default serializer: {elapsedOriginal}ms, {elapsedOriginal / itemCount} per item");
            Console.WriteLine($"Custom serializer: {elapsedCustom}ms, {elapsedCustom / itemCount} per item");
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