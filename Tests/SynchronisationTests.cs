using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Main.Databases;
using Main.Databases.Utility;
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
            Client1 = new TestClient(Localhost, port);
            Client2 = new TestClient(Localhost, port);
            Client3 = new TestClient(Localhost, port);
            
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
        public static void TestListSet()
        {
            Setup(nameof(TestListSet));
            string id = nameof(TestListSet);
            
            //set value in database 1
            Database1.Add<List<int>, int>(id, 25);
            
            //wait for synchronisation in databases 2 and 3
            TestUtility.AreEqual(new List<int>(){25}, (() => Database2.Get<List<int>>(id)), "Test remote set after first get");
            TestUtility.AreEqual(new List<int>(){25}, (() => Database3.Get<List<int>>(id)), "Test remote set before first get");
        }

        [Test]
        public static void TestConcurrentSets()
        {
            Setup(nameof(TestConcurrentSets));
            string id = nameof(TestConcurrentSets);
            
            const int setCount = 200;

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
            
            const int addCount = 1000;
            
            //throw exception if value is overwritten during execution
            Database1.AddCallback<List<int>>(id, value =>
            {
                if (value == null || value.Count == 0) throw new Exception("Add was null or empty!");
            });

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
                        Database2.Add<List<int>, int>(id, i + addCount);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 3");
                    
                    for (int i = 0; i < addCount; i++)
                    {
                        Database3.Add<List<int>, int>(id, i + (addCount * 2));
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

            //wait for internal tasks to complete
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(0, (() => database.Scheduler.QueuedTasksCount), "Internal tasks", 5000);
            }
            Console.WriteLine($"All adds completed internally after {stopwatch.ElapsedMilliseconds} ms");

            //make sure data was synchronised in all databases
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(addCount * 3, () => database.Get<List<int>>(id)?.Count, "ElementCount");
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

        [Test]
        public static void TestSafeModifySimple()
        {
            Setup(nameof(TestSafeModifySimple));
            string id = nameof(TestSafeModifySimple);
            
            Database1.SafeModify<int>(id, (value) => 100);
            
            TestUtility.AreEqual(100, () => Database1.Get<int>(id));
            TestUtility.AreEqual(100, () => Database2.Get<int>(id));
            TestUtility.AreEqual(100, () => Database3.Get<int>(id));
            
            Assert.AreEqual(1, Database1.GetModCount(id));
            Assert.AreEqual(1, Database2.GetModCount(id));
            Assert.AreEqual(1, Database3.GetModCount(id));
            
            Assert.AreEqual(0, Database1.GetOngoingSets(id));
            Assert.AreEqual(0, Database2.GetOngoingSets(id));
            Assert.AreEqual(0, Database3.GetOngoingSets(id));
        }
        
        [Test]
        public static void TestSafeModify()
        {
            Setup(nameof(TestSafeModify));
            string id = nameof(TestSafeModify);
            
            //test options
            const int addCount = 100;
            
            //track added values
            List<int> addedValues = new List<int>(addCount * 3);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Task[] tasks = new[]
            {
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database1.SafeModify<List<int>>(id, (value) =>
                        {
                            value ??= new List<int>(); 
                            value.Add(value.Count);
                            
                            Assert.IsFalse(addedValues.Contains(value.Count));
                            addedValues.Add(value.Count);
                            
                            return value;
                        });
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database2.SafeModify<List<int>>(id, (value) =>
                        {
                            value ??= new List<int>(); 
                            value.Add(value.Count);
                            
                            Assert.IsFalse(addedValues.Contains(value.Count));
                            addedValues.Add(value.Count);
                            
                            return value;
                        });
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database3.SafeModify<List<int>>(id, (value) =>
                        {
                            value ??= new List<int>(); 
                            value.Add(value.Count);
                            
                            Assert.IsFalse(addedValues.Contains(value.Count));
                            addedValues.Add(value.Count);
                            
                            return value;
                        });
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
            Assert.IsTrue(Task.WaitAll(tasks, 3000));
            Console.WriteLine($"All adds completed after {stopwatch.ElapsedMilliseconds} ms");

            //wait for internal tasks to complete
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(0, (() => database.Scheduler.QueuedTasksCount), "Internal tasks", 5000);
            }
            Console.WriteLine($"All adds completed internally after {stopwatch.ElapsedMilliseconds} ms");
            
            //make sure data was synchronised in all databases
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(addCount * 3, () => database.Get<List<int>>(id)?.Count, "ElementCount");
                TestUtility.AreEqual(true, (() =>
                {
                    List<int> list = database.Get<List<int>>(id);
                    for (int i = 0; i < addCount * 3; i++)
                    {
                        if(list[i] == i) continue;
                    
                        Console.WriteLine($"List doesn't contain {i} at right index");
                        return false;
                    }
                    return true;
                }), "Value Order");
            }
            
            stopwatch.Stop();
            Console.WriteLine($"All adds completed after: {stopwatch.ElapsedMilliseconds} ms");
            
            Console.WriteLine(LogWriter.StringifyCollection(Database1.Get<List<int>>(id)));
            
            Assert.AreEqual(addCount * 3, Database1.GetModCount(id));
            Assert.AreEqual(addCount * 3, Database2.GetModCount(id));
            Assert.AreEqual(addCount * 3, Database3.GetModCount(id));
            
            Assert.AreEqual(0, Database1.GetOngoingSets(id));
            Assert.AreEqual(0, Database2.GetOngoingSets(id));
            Assert.AreEqual(0, Database3.GetOngoingSets(id));
        }

        [Test]
        public static void TestSafeModifySync()
        {
            string id = nameof(TestSafeModifySync);
            Setup(id);

            for (int i = 1; i <= 10; i++)
            {
                Assert.AreEqual(new List<int>(){1}, Database1.SafeModifySync<List<int>>(id, (value =>
                {
                    Console.WriteLine($"Executing SafeModify1. CurrentValue: {LogWriter.StringifyCollection(value)}");
                    return new List<int>() { 1 };
                })), "SafeModify");
                Assert.AreEqual(new List<int>(){1,2}, Database2.SafeModifySync<List<int>>(id, (value =>
                {
                    Console.WriteLine($"Executing SafeModify2. CurrentValue: {LogWriter.StringifyCollection(value)}");
                    value.Add(2);
                    return value;
                })), "SafeModify");
                Assert.AreEqual(new List<int>(){1,2,3}, Database3.SafeModifySync<List<int>>(id, (value =>
                {
                    Console.WriteLine($"Executing SafeModify3. CurrentValue: {LogWriter.StringifyCollection(value)}");
                    value.Add(3);
                    return value;
                })),"SafeModify");
                
                TestUtility.AreEqual((uint) i * 3, () => Database1.GetModCount(id), "ModCount");
                TestUtility.AreEqual((uint) i * 3, () => Database2.GetModCount(id), "ModCount");
                TestUtility.AreEqual((uint) i * 3, () => Database3.GetModCount(id), "ModCount");
                
                TestUtility.AreEqual(0, () => Database1.GetOngoingSets(id), "OngoingSets");
                TestUtility.AreEqual(0, () => Database2.GetOngoingSets(id), "OngoingSets");
                TestUtility.AreEqual(0, () => Database3.GetOngoingSets(id), "OngoingSets");
                
                Console.WriteLine($"Completed iteration {i}");
            }
        }
    }
}