using System;
using System.Threading;
using DMP.Databases;
using NUnit.Framework;

namespace Tests
{
    public static class CallbackTests
    {
        [Test]
        public static void TestCallbacks()
        {
            string id = nameof(TestCallbacks);
            string lastCallbackSet = "string with length 21";
            
            Database database = new Database(id);
            
            //save string length in second database variable
            database.AddCallback<string>("string", (data) =>
            {
                database.SetValue("stringLength", data.Length);
            });

            //test if callback was added
            foreach (var input in new []{"", "test", "my master", "fck yeah boy", lastCallbackSet})
            {
                database.SetValue("string", input);
                TestUtility.AreEqual(input.Length, () =>
                {
                    return database.GetValue<int>("stringLength");
                });
            }
            
            //test if callback was removed
            Assert.AreEqual(1, database.RemoveCallbacks("string"));
            
            database.SetValue("string", "42");
            //wait for callbacks to execute
            TestUtility.AreEqual(0, () => database.QueuedTasksCount, "Callbacks are executed");
            
            //make sure string length is still 21
            Assert.AreEqual(lastCallbackSet.Length, database.GetValue<int>("stringLength"));
        }

        [Test]
        public static void TestUniqueParameter()
        {
            string id = nameof(TestUniqueParameter);
            int invokationCount = 0;
            
            Database database = new Database(id);

            for (int i = 0; i < 10; i++)
            {
                database.AddCallback<string>(id, s =>
                {
                    Console.WriteLine("Invoked callback!");
                    invokationCount++;
                }, unique: true, invokeCallback:true);    
            }
            
            Assert.AreEqual(1, invokationCount);
        }

        [Test]
        public static void TestRemoveCallbackOnException()
        {
            int causedExceptions = 0;
            
            string id = nameof(TestRemoveCallbackOnException);
            Database database = new Database(id);
            database.AddCallback<string>(id, (value) =>
            {
                causedExceptions++;
                throw new NotImplementedException();
            }, removeOnError: true);
            
            database.SetValue(id, "Test");
            database.SetValue(id, "Test2");
            
            Thread.Sleep(1000);
            
            Assert.AreEqual(1, causedExceptions);
            
            Assert.AreEqual(0, database.GetCallbackCount(id));
        }
    }
}