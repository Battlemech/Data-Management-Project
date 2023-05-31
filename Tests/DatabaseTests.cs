using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DMP.Callbacks;
using DMP.Databases;
using DMP.Databases.Utility;
using DMP.Networking;
using DMP.Networking.Synchronisation.Server;
using DMP.Utility;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
    public static class BaseTests
    {
        [Test]
        public static void TestGetNull()
        {
            Database database = new Database(nameof(TestGetNull));
            Assert.AreEqual(0, database.GetValue<int>(""));
            Assert.AreEqual(0, database.GetValue<int>(""));
        }

        [Test]
        public static void TestAdd()
        {
            string id = nameof(TestAdd);
            
            Database database = new Database(id);
            database.Add<List<string>, string>(id, id);
            
            Assert.AreEqual(database.GetValue<List<string>>(id)[0], id);
        }

        [Test]
        public static void TestRemove()
        {
            string id = nameof(TestRemove);

            Database database = new Database(id);
            database.SetValue(id, new List<string>(){id});
            database.Remove<List<string>, string>(id, id);
            
            Assert.AreEqual(0, database.GetValue<List<string>>(id).Count);
        }

        [Test]
        public static void TestOnInitialized()
        {
            string id = nameof(TestOnInitialized);

            Database database = new Database(id);
            database.OnInitialized<string>(id, (s =>
            {
                database.SetValue(id+id, id);
            }));
            
            //trigger onInitialized
            database.SetValue(id, "yeah");
            
            //test delayed trigger
            TestUtility.AreEqual(id, () => database.GetValue<string>(id+id));
            
            //test trigger if default
            database.OnInitialized<int>("1", i =>
            {
                database.SetValue("2", i + 1);
                Console.WriteLine($"OnInitialized was triggered. Set id:2={i+1}");
            });
            database.SetValue<int>("1", default);
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.QueuedTasksCount);
            Assert.AreEqual(0, database.GetValue<int>("2"));
            
            //set value to 1, not invoking OnInitialized
            database.SetValue<int>("1", 0); //doesn't trigger because its default value
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.QueuedTasksCount);
            Assert.AreEqual(0, database.GetValue<int>("2"));
            
            //set value to 1, invoking OnInitialized
            database.SetValue<int>("1", 1); 
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.QueuedTasksCount);
            TestUtility.AreEqual(2, () => database.GetValue<int>("2"));
        }

        [Test]
        public static async Task TestExceptionHandling()
        {
            Database database = new Database("Test");

            //raise exception when callback is triggered
            database.AddCallback<string>("Id", (value) => throw new NotImplementedException());

            try
            {
                await database.Get<string>("Id").Set("Test");
            }
            catch (NotImplementedException)
            {
                return;
            }
            
            Assert.Fail("Failed to trigger not implemented exception");
        }

        [Test]
        public static void TestCallbackHandler()
        {
            CallbackHandler<int> callbackHandler = new CallbackHandler<int>();
            string toInvoke = "Test";
            string invoked = "";
            
            //test simple callback
            callbackHandler.AddCallback<string>(0, (s => invoked = s));
            callbackHandler.InvokeCallbacks(0, "Test");
            
            Assert.AreEqual(toInvoke, invoked);
            
            //make sure it was added correctly
            Assert.AreEqual(1, callbackHandler.GetCallbackCount(0));
            
            //add faulty callback
            callbackHandler.AddCallback<string>(0, (s => throw new NotImplementedException()));
            
            //invoke callback
            callbackHandler.InvokeCallbacks(0, toInvoke);
            
            //make sure callback wasn't removed
            Assert.AreEqual(2, callbackHandler.GetCallbackCount(0));
            
            //add self removing callback
            callbackHandler.AddCallback<string>(0, (s => throw new NotImplementedException()), removeOnError:true);
            Assert.AreEqual(3, callbackHandler.GetCallbackCount(0));
            
            //remove it by evoking it
            callbackHandler.InvokeCallbacks(0, toInvoke);
            Assert.AreEqual(2, callbackHandler.GetCallbackCount(0));
            
            //try adding callback with duplicate name
            Assert.IsTrue(callbackHandler.AddCallback<string>(1, (Console.WriteLine), unique: true));
            Assert.AreEqual(1, callbackHandler.GetCallbackCount(1));
            Assert.IsFalse(callbackHandler.AddCallback<string>(1, (Console.WriteLine), unique: true));
            Assert.AreEqual(1, callbackHandler.GetCallbackCount(1));
        }

        [Test]
        public static void TestCallbackHandlerBytes()
        {
            string test = "ABC";
            string invoked = "";
            CallbackHandler<int> callbackHandler = new CallbackHandler<int>();

            callbackHandler.AddCallback<string>(0, (s => invoked = s));
            callbackHandler.UnsafeInvokeCallbacks(0, Serialization.Serialize(test), typeof(string));
            
            Assert.AreEqual(test, invoked);
        }
        
    }
}