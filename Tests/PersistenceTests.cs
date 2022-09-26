using System;
using System.Collections.Generic;
using System.Threading;
using DMP.Databases;
using DMP.Databases.Utility;
using DMP.Databases.ValueStorage;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Server;
using DMP.Objects;
using DMP.Persistence;
using NUnit.Framework;

namespace Tests
{
    public static class PersistenceTests
    {
        [Test]
        public static void TestPersistence()
        {
            string id = nameof(TestPersistence);
            
            //make sure the test is unaffected by old data
            PersistentData.DeleteDatabase(id);

            for (int i = 0; i < 10; i++)
            {
                Database database = new Database(id, true);
                
                //load expected old value
                Assert.AreEqual(i, database.GetValue<int>(id));
                
                database.SetValue(id, i + 1);

                //make sure the value has been updated correctly in database
                Assert.AreEqual(i + 1, database.GetValue<int>(id));
                
                //make sure the value was saved correctly in persistent data
                TestUtility.AreEqual(true, () => PersistentData.TryLoad(id, id, out int value) && value == i + 1, "PersistentSave");
            }
        }

        [Test]
        public static void TestSyncRequired()
        {
            int port = TestUtility.GetPort(nameof(PersistenceTests), nameof(TestSyncRequired));
            string id = nameof(TestSyncRequired);
            PersistentData.DeleteDatabase(id);

            //create client which allows synchronised database to send data
            SynchronisedServer server = new SynchronisedServer("127.0.0.1", port);
            server.Start();
            SynchronisedClient client = new SynchronisedClient("127.0.0.1", port);
            client.ConnectAsync();
            Assert.IsTrue(client.WaitForConnect());
            
            //create persistent and synchronised database
            Database database = new Database(id, true, true);
            database.SetValue(id, false);

            //database was synchronised. Sync not required
            TestUtility.AreEqual(0, (() => database.Scheduler.QueuedTasksCount), //todo: why does this take so long?
                "Internal database tasks", 15000);
            TestUtility.AreEqual(0, () => PersistentData.DataToSaveCount,
                "Persistent data write", 15000);
            Thread.Sleep(100); //wait for transaction to complete
            TestUtility.AreEqual(false, () => PersistentData.SyncRequired(id, id),
                "sync required", 15000);

            //disable synchronisation
            database.IsSynchronised = false;
            database.SetValue(id, true);
            
            //database wasn't synchronised. Sync required
            TestUtility.AreEqual(0, (() => database.Scheduler.QueuedTasksCount),  //todo: why does this take so long?
                "Internal database tasks", 15000);
            TestUtility.AreEqual(0, () => PersistentData.DataToSaveCount,
                "Persistent data write", 15000);
            Thread.Sleep(100); //wait for transaction to complete
            TestUtility.AreEqual(true, () => PersistentData.SyncRequired(id, id),
                "sync required", 15000);
        }

        private static Guid _lastGuid = default;
        
        [Test]
        public static void LoadGuid()
        {
            //delete old Guid
            SystemDatabase.DeleteData();
            
            Guid guid = SystemDatabase.Guid;
            
            Assert.AreNotEqual(guid, _lastGuid);

            _lastGuid = guid;

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(guid, SystemDatabase.Guid);
            }
            Console.WriteLine(guid);
        }

        [Test]
        public static void OfflineModification()
        {
            string id = "IntList";
            
            //setup networking
            int port = TestUtility.GetPort(nameof(PersistenceTests), nameof(OfflineModification));
            SynchronisedServer server = new SynchronisedServer("127.0.0.1", port);
            server.Start();

            //create two clients
            TestClient client1 = new TestClient(port);
            TestClient client2 = new TestClient(port);

            //create two databases
            Database database1 = new Database("DB") { Client = client1};
            Database database2 = new Database("DB") { Client = client2 };

            //set values -> offline sets
            database1.SetValue(id, new List<int>(){1, 2});
            database2.SetValue(id, new List<int>(){3});
            
            Assert.AreEqual(0, database1.Scheduler.QueuedTasksCount);
            Assert.AreEqual(0, database2.Scheduler.QueuedTasksCount);
            
            //connect clients
            client1.ConnectAsync();
            client2.ConnectAsync();
            
            Assert.IsTrue(client1.WaitForConnect());
            Assert.IsTrue(client2.WaitForConnect());

            //make databases synchronised
            database1.IsSynchronised = true;
            database2.IsSynchronised = true;

            //make sure client 1 is host
            TestUtility.AreEqual(1, () =>
            {
                int hostCount = 0;
                if (database1.IsHost) hostCount++;
                if (database2.IsHost) hostCount++;
                return hostCount;
            }, "One host");
            
            Console.WriteLine($"Database {((database1.IsHost) ? "1" : "2")} is host");

            if(database1.IsHost) TestUtility.AreEqual(new List<int>(){1, 2}, () => database2.GetValue<List<int>>(id));
            else if(database2.IsHost) TestUtility.AreEqual(new List<int>(){3}, () => database1.GetValue<List<int>>(id));
            else Assert.Fail("No database is host");
        }
        
        [Test]
        public static void TestPersistentModify()
        {
            Database database = new Database("Test", true);
            database.Modify<int>("Test", value =>
            {
                Console.WriteLine(value);
                return value + 1;
            });
            Console.WriteLine(database.GetValue<int>("Test"));
            
            TestUtility.AreEqual(true, (() => PersistentData.TryLoad("Test", "Test", out int value) && value == 1));
            
            //load database again
            Database loaded = new Database("Test", true);
            Assert.AreEqual(1, loaded.GetValue<int>("Test"));
        }

        [Test]
        public static void TestPersistenceAfterSet()
        {
            string id = nameof(TestPersistenceAfterSet);
            Database database = new Database(id);
            database.SetValue(id, id);

            database.IsPersistent = true;
            Thread.Sleep(1000);

            database = new Database(id, true);
            Assert.AreEqual(id, database.GetValue<string>(id));
        }

        [Test]
        public static void TestSynchronisedObjectOfflineSaving()
        {
            string id = nameof(TestSynchronisedObjectOfflineSaving);
            PersistentData.DeleteDatabase(id);

            for (int i = 0; i < 10; i++)
            {
                if (i != 0)
                {
                    TestUtility.AreEqual(true, () => PersistentData.DoesDatabaseExist(id));
                    TestUtility.AreEqual(true, () => PersistentData.DoesDatabaseExist(Environment.UserName));
                }
                
                SynchronisedClient client = new TestClient();
                Database database = new Database(id, true)
                {
                    Client = client
                };

                ValueStorage<PlayerData> playerData = database.Get<PlayerData>(id);
                
                if(i == 0) Assert.AreEqual(null, playerData.Get());
                else Assert.AreNotEqual(null, playerData.Get());
                
                playerData.Modify((value =>
                {
                    //init player data if necessary
                    if (value == null)
                    {
                        value = new PlayerData(Environment.UserName);
                    }
                    
                    //increment its mod count
                    value.AccessCount.Modify((currentValue => currentValue + 1));
                    return value;
                }));

                Assert.AreEqual(i + 1, playerData.Get().AccessCount.Get());
                Console.WriteLine($"Iteration {i} succeeded");
            }
        }

        [Test]
        public static void TestSynchronisedObjectSerialization()
        {
            SynchronisedClient client = new TestClient();
            
            for (int i = 0; i < 10; i++)
            {
                PlayerData playerData = new PlayerData("Test");
                
                Assert.AreEqual(i, playerData.AccessCount.Get());
                playerData.AccessCount.Modify((value => value + 1));
                Assert.AreEqual(i + 1, playerData.AccessCount.Get());

                TestUtility.AreEqual(true, (() => PersistentData.DoesDatabaseExist("Test")), "Database was saved");
                TestUtility.AreEqual(true, () =>
                {
                    bool success = PersistentData.TryLoad("Test", nameof(playerData.AccessCount), out int value);

                    if (!success) return false;

                    return value == i + 1;
                }, "Value was saved");

                //de-initialize client
                playerData.GetDatabase().Client = null;
            }
        }
        
        private class PlayerData : SynchronisedObject
        {
            public PlayerData(string databaseId) : base(databaseId, true)
            {
                Console.WriteLine($"Created player data {databaseId}");
            }

            public ValueStorage<int> AccessCount => GetDatabase().Get<int>(nameof(AccessCount));
        }
    }
}