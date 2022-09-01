using System;
using System.Collections.Generic;
using System.Threading;
using Main.Databases;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Server;
using Main.Persistence;
using Main.Utility;
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
                Assert.AreEqual(i, database.Get<int>(id));
                
                database.Set(id, i + 1);

                //make sure the value has been updated correctly in database
                Assert.AreEqual(i + 1, database.Get<int>(id));
                
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
            database.Set(id, false);

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
            database.Set(id, true);
            
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
            Database database1 = new Database("DB", true);
            Database database2 = new Database("DB") { Client = client2 };

            //set values -> offline sets
            database1.Set(id, new List<int>(){1, 2});
            database2.Set(id, new List<int>(1));
            
            //connect clients
            client1.ConnectAsync();
            client2.ConnectAsync();
            
            Assert.IsTrue(client1.WaitForConnect());
            Assert.IsTrue(client2.WaitForConnect());
            
            Console.WriteLine($"Values before IsSynchronised=true: Host=1:{LogWriter.StringifyCollection(database1.Get<List<int>>(id))}, 2:{LogWriter.StringifyCollection(database2.Get<List<int>>(id))}");
            
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
            
            if(database1.IsHost) TestUtility.AreEqual(new List<int>(){1, 2}, () => database2.Get<List<int>>(id));
            else if(database2.IsHost) TestUtility.AreEqual(new List<int>(){1}, () => database2.Get<List<int>>(id));
            else Assert.Fail("No database is host");
        }
    }
}