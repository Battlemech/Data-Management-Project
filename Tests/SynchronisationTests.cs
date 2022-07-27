using System;
using System.Threading;
using Main.Databases;
using Main.Networking.Synchronisation;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Server;
using NUnit.Framework;

namespace Tests
{
    public static class SynchronisationTests
    {
        public const string Localhost = "127.0.0.1";
        public static SynchronisedServer Server;
        public static SynchronisedClient Client1;
        public static SynchronisedClient Client2;
        public static SynchronisedClient Client3;
        public static Database Database1;
        public static Database Database2;
        public static Database Database3;

        public static void Setup(string testName)
        {
            int port = TestUtility.GetPort(nameof(NetworkingTests), testName);
            
            //setup networking
            Server = new SynchronisedServer(Localhost, port);
            Client1 = new SynchronisedClient(Localhost, port);
            Client2 = new SynchronisedClient(Localhost, port);
            Client3 = new SynchronisedClient(Localhost, port);
            
            //setup databases
            Database1 = new Database(Localhost, false, false);
            Database2 = new Database(Localhost, false, false);
            Database3 = new Database(Localhost, false, false);
            
            //set clients and enable synchronisation for databases
            Database1.Client = Client1;
            Database1.IsSynchronised = true;
            
            Database2.Client = Client2;
            Database2.IsSynchronised = true;
            
            Database3.Client = Client3;
            Database3.IsSynchronised = true;
            
            //start server and clients
            Assert.IsTrue(Server.Start());
            Assert.IsTrue(Client1.ConnectAsync());
            Assert.IsTrue(Client2.ConnectAsync());
            Assert.IsTrue(Client3.ConnectAsync());
            
            //wait until connection is established
            Assert.IsTrue(Client1.WaitForConnect());
            Assert.IsTrue(Client2.WaitForConnect());
            Assert.IsTrue(Client3.WaitForConnect());
        }

        [TearDown]
        public static void TearDown()
        {
            Assert.IsTrue(Server.Stop());

            Client1.DisconnectAsync();
            Client2.DisconnectAsync();
            Client3.DisconnectAsync();
            
        }

        [Test]
        public static void TestSetup()
        {
            Setup(nameof(TestSetup));
        }
        
        [Test]
        public static void TestSimpleSet()
        { 
            Setup(nameof(TestSimpleSet));
            
            string id = nameof(TestSimpleSet);
            
            //set value in database 1
            Database1.Set(id, id);
            
            //wait for synchronisation in databases 2 and 3
            TestUtility.AreEqual(id, (() => Database2.Get<string>(id)), "Test remote set after first get");
            TestUtility.AreEqual(id, (() => Database3.Get<string>(id)), "Test remote set before first get");
        } 
    }
}