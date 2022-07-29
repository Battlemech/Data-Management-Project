using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Main.Databases;
using Main.Databases.Utility;
using Main.Networking.Synchronisation;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Server;
using Main.Utility;
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

        [Test]
        public static void TestConcurrentSets()
        {
            Setup(nameof(TestConcurrentSets));
            string id = nameof(TestConcurrentSets);
            
            const int setCount = 20;

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Task[] tasks = new[]
            {
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 1");
                    
                    for (int i = 0; i < setCount; i++)
                    {
                        Database1.Set(id, i);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 2");
                    
                    for (int i = 0; i < setCount; i++)
                    {
                        Database2.Set(id, -i);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 3");
                    
                    for (int i = 0; i < setCount; i++)
                    {
                        Database3.Set(id, i * setCount);
                    }
                }))
            };

            //start setting value
            foreach (var task in tasks)
            {
                task.Start();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            
            //start set process
            resetEvent.Set();
            
            //make sure all task terminated
            Assert.IsTrue(Task.WaitAll(tasks, 10000));
            
            Console.WriteLine($"All sets completed after {stopwatch.ElapsedMilliseconds} ms");
            
            //check all values
            TestUtility.AreEqual(true, (() =>
            {
                int a = Database1.Get<int>(id);
                int b = Database2.Get<int>(id);
                int c = Database3.Get<int>(id);
                return a == b && b == c;
            }), "values are equal", 10000 );
            
            //make sure all requests were processed
            TestUtility.AreEqual(0, (() => Database1.GetOngoingSets(id)));
            TestUtility.AreEqual(0, (() => Database2.GetOngoingSets(id)));
            TestUtility.AreEqual(0, (() => Database3.GetOngoingSets(id)));
            
            //check all values
            TestUtility.AreEqual(true, (() =>
            {
                int a = Database1.Get<int>(id);
                int b = Database2.Get<int>(id);
                int c = Database3.Get<int>(id);
                return a == b && b == c;
            }), "values are equal", 10000 );

            stopwatch.Stop();
            Console.WriteLine($"Synchronisation completed after {stopwatch.ElapsedMilliseconds} ms");
        }

        [Test]
        public static void TestConcurrentAdd()
        {
            Setup(nameof(TestConcurrentAdd));
            string id = nameof(TestConcurrentAdd);
            
            const int addCount = 100;

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Task[] tasks = new[]
            {
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 1");
                    
                    for (int i = 0; i < addCount; i++)
                    {
                        Database1.Add<List<int>, int>(id, i);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 2");
                    
                    for (int i = 0; i < addCount; i++)
                    {
                        Database1.Add<List<int>, int>(id, i + addCount);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 3");
                    
                    for (int i = 0; i < addCount; i++)
                    {
                        Database1.Add<List<int>, int>(id, i + (addCount * 2));
                    }
                }))
            };

            //start setting value
            foreach (var task in tasks)
            {
                task.Start();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            
            //start set process
            resetEvent.Set();
            
            //make sure all task terminated
            Assert.IsTrue(Task.WaitAll(tasks, 10000));
            
            Console.WriteLine($"All adds completed after {stopwatch.ElapsedMilliseconds} ms");

            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(addCount * 3, () => database.Get<List<int>>(id)?.Count);
                TestUtility.AreEqual(true, (() =>
                {
                    List<int> list = database.Get<List<int>>(id);
                    for (int i = 0; i < addCount * 3; i++)
                    {
                        if(list.Contains(i)) continue;
                    
                        Console.WriteLine($"List doesn't contain {i}");
                        return false;
                    }
                    return true;
                }));
            }
            
            stopwatch.Stop();
            Console.WriteLine($"All adds completed after: {stopwatch.ElapsedMilliseconds} ms");
            
            Console.WriteLine(LogWriter.StringifyCollection(Database1.Get<List<int>>(id)));
            
        }
    }
}