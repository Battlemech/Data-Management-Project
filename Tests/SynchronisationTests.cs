using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DMP.Databases;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Server;
using NUnit.Framework;

namespace Tests
{
    public static class SynchronisationTests
    {
        [Test]
        public static void TestSimpleSet()
        {
            int clientCount = 10;

            List<SynchronisedClient> clients = TestUtility.ConnectClients(out SynchronisedServer server, out List<Database> databases, clientCount);
            
            databases[0].Get<int>("int").Set(12);
            
            Console.WriteLine($"Updated value in database {databases[0]}");
            
            AssertSameValue("int", 12, databases);

            server.Stop();
        }

        [Test]
        public static void TestSimultaneouslySet()
        {
            int clientCount = 3;

            List<SynchronisedClient> clients = TestUtility.ConnectClients(out SynchronisedServer server, out List<Database> databases, clientCount);
            
            ManualResetEvent start = new ManualResetEvent(false);

            for (int i = 0; i < clientCount; i++)
            {
                var i1 = i;
                Task task = new Task((() =>
                {
                    start.WaitOne();
                    databases[i1].Get<int>("int").BlockingSet((current => current + 1));
                }));
                task.Start();
            }
            
            //stat setting values simultaneously
            start.Set();
            
            AssertSameValue("int", clientCount, databases);

            server.Stop();
        }

        public static void AssertSameValue<T>(string valueId, T expected, List<Database> databases)
        {
            foreach (var database in databases)
            {
                TestUtility.AreEqual(expected, () => database.Get<T>(valueId).Get());
            }
        }
    }
}