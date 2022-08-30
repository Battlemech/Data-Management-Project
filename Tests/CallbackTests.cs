using System;
using Main.Databases;
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
                database.Set("stringLength", data.Length);
            });

            //test if callback was added
            foreach (var input in new []{"", "test", "my master", "fck yeah boy", lastCallbackSet})
            {
                database.Set("string", input);
                TestUtility.AreEqual(input.Length, () =>
                {
                    return database.Get<int>("stringLength");
                });
            }
            
            //test if callback was removed
            Assert.AreEqual(1, database.RemoveCallbacks("string"));
            
            database.Set("string", "42");
            //wait for callbacks to execute
            TestUtility.AreEqual(0, () => database.Scheduler.QueuedTasksCount, "Callbacks are executed");
            
            //make sure string length is still 21
            Assert.AreEqual(lastCallbackSet.Length, database.Get<int>("stringLength"));
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
    }
}