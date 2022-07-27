using System;
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
        public static readonly SynchronisedServer Server = new SynchronisedServer(Localhost);
        public static readonly SynchronisedClient Client1 = new SynchronisedClient(Localhost);
        public static readonly SynchronisedClient Client2 = new SynchronisedClient(Localhost);
        public static readonly SynchronisedClient Client3 = new SynchronisedClient(Localhost);
        public static readonly Database Database1 = new Database(Localhost, false, false);
        public static readonly Database Database2 = new Database(Localhost, false, false);
        public static readonly Database Database3 = new Database(Localhost, false, false);

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            //set clients and enable synchronisation for databases
            Database1.Client = Client1;
            Database1.IsSynchronised = true;
            
            Database2.Client = Client2;
            Database2.IsSynchronised = true;
            
            Database3.Client = Client3;
            Database3.IsSynchronised = true;
        }
        
        [SetUp]
        public static void Setup()
        {
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
            
        }
        
        [Test]
        public static void TestSimpleSet()
        { 
            string id = nameof(TestSimpleSet);
            
            //set value in database 1
            Database1.Set(id, id);
            
            //wait for synchronisation in databases 2 and 3
            TestUtility.AreEqual(id, (() => Database2.Get<string>(id)));
            TestUtility.AreEqual(id, (() => Database3.Get<string>(id)));
        } 
    }
}