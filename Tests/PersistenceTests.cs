using System;
using System.Collections.Generic;
using System.Threading;
using Main.Databases;
using Main.Networking.Synchronisation;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Server;
using Main.Persistence;
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
            TestUtility.AreEqual(true, () => PersistentData.SyncRequired(id, id),
                "sync required", 15000);
        }

        [Test]
        public static void LoadGuid()
        {
            Guid guid = LocalDatabase.LoadGuid();
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(guid, LocalDatabase.LoadGuid());
            }
        }
    }
}