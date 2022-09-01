using System;
using System.Collections.Generic;
using System.Threading;
using Main.Databases;
using Main.Databases.Utility;
using NUnit.Framework;

namespace Tests
{
    public static class BaseTests
    {
        [Test]
        public static void TestGetNull()
        {
            Database database = new Database(nameof(TestGetNull));
            Assert.AreEqual(0, database.Get<int>(""));
            Assert.AreEqual(0, database.Get<int>(""));
        }

        [Test]
        public static void TestAdd()
        {
            string id = nameof(TestAdd);
            
            Database database = new Database(id);
            database.Add<List<string>, string>(id, id);
            
            Assert.AreEqual(database.Get<List<string>>(id)[0], id);
        }

        [Test]
        public static void TestRemove()
        {
            string id = nameof(TestRemove);

            Database database = new Database(id);
            database.Set(id, new List<string>(){id});
            database.Remove<List<string>, string>(id, id);
            
            Assert.AreEqual(0, database.Get<List<string>>(id).Count);
        }

        [Test]
        public static void TestOnInitialized()
        {
            string id = nameof(TestOnInitialized);

            Database database = new Database(id);
            database.OnInitialized<string>(id, (s =>
            {
                database.Set(id+id, id);
            }));
            
            //trigger onInitialized
            database.Set(id, "yeah");
            
            //test delayed trigger
            TestUtility.AreEqual(id, () => database.Get<string>(id+id));
            
            //test trigger if default
            database.OnInitialized<int>("1", i =>
            {
                database.Set("2", i + 1);
                Console.WriteLine($"OnInitialized was triggered. Set id:2={i+1}");
            });
            database.Set<int>("1", default);
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.Scheduler.QueuedTasksCount);
            Assert.AreEqual(0, database.Get<int>("2"));
            
            //set value to 1, not invoking OnInitialized
            database.Set<int>("1", 0); //doesn't trigger because its default value
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.Scheduler.QueuedTasksCount);
            Assert.AreEqual(0, database.Get<int>("2"));
            
            //set value to 1, invoking OnInitialized
            database.Set<int>("1", 1); 
            
            //wait until callback from set was triggered
            TestUtility.AreEqual(0, () => database.Scheduler.QueuedTasksCount);
            TestUtility.AreEqual(2, () => database.Get<int>("2"));
        }
        
    }
}