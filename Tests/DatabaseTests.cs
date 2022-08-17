using System;
using System.Collections.Generic;
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
        
    }
}