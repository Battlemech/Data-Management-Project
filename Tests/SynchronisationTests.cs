using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Main;
using Main.Databases;
using Main.Databases.Utility;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;
using Main.Networking.Synchronisation.Server;
using Main.Persistence;
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
            Client1 = new TestClient(port);
            Client2 = new TestClient(port);
            Client3 = new TestClient(port);

            //start server and clients
            Assert.IsTrue(Server.Start());
            Assert.IsTrue(Client1.ConnectAsync());
            Assert.IsTrue(Client2.ConnectAsync());
            Assert.IsTrue(Client3.ConnectAsync());
            
            //wait until connection is established
            Assert.IsTrue(Client1.WaitForConnect());
            Assert.IsTrue(Client2.WaitForConnect());
            Assert.IsTrue(Client3.WaitForConnect());
            
            
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
            
            const int addCount = 2000;
            
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
                TestUtility.IsChanging(0, () => database.Scheduler.QueuedTasksCount, "Internal tasks");
            }
            Console.WriteLine($"All adds completed internally after {stopwatch.ElapsedMilliseconds} ms");

            //make sure data was synchronised in all databases
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(true, (() => database.Get<List<int>>(id) != null), "List is not null");
                TestUtility.IsChanging(addCount * 3, () => database.Get<List<int>>(id).Count, "ElementCount");
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
                
                Assert.AreEqual(addCount * 3, database.Get<List<int>>(id).Count, "ElementCount - Late check");
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

        [Test]
        public static void TestSimpleConnect()
        {
            string id = nameof(TestSimpleConnect);
            Setup(id);
            
            //disconnect client 1
            Assert.IsTrue(Client1.DisconnectAsync());
            
            //set value
            Database2.Set(id, id);
            
            //wait for modification
            TestUtility.AreEqual(id, () => Database3.Get<string>(id));
            Database3.Modify<string>(id, (value) =>
            {
                Console.WriteLine($"Database3: Modifying: {value}. Adding:4");
                return value + "4";
            });
            
            //wait for value to be synchronised in network
            TestUtility.AreEqual(id+"4", () => Database3.Get<string>(id), "Synchronisation before test");
            TestUtility.AreEqual(id+"4", () => Database2.Get<string>(id), "Synchronisation before test");
            TestUtility.AreEqual((uint) 2, () => Server.GetModCount(Database1.Id, id));
            
            //reconnect client 1
            Assert.IsTrue(Client1.ConnectAsync());
            Assert.IsTrue(Client1.WaitForConnect());
            
            //wait for value to be synchronised on previously disconnected client
            TestUtility.AreEqual(id+"4", () => Database1.Get<string>(id), "Synchronisation of value set before client connected");
            TestUtility.AreEqual((uint) 2, (() => Database1.GetModCount(id)));
            
            //try modifying result as newly connected client
            Database1.Modify<string>(id, (value) => value + "5");
            
            TestUtility.AreEqual(id+"45", (() => Database2.Get<string>(id)));
            TestUtility.AreEqual(id+"45", (() => Database3.Get<string>(id)));
            
            //make sure modCount was tracked correctly
            Assert.AreEqual(3, Database1.GetModCount(id));
            Assert.AreEqual(3, Database2.GetModCount(id));
            Assert.AreEqual(3, Database3.GetModCount(id));
        }

        [Test]
        public static void TestConnectDuringModification()
        {
            string id = nameof(TestConnectDuringModification);
            Setup(id);
            
            //disconnect client 1
            Assert.IsTrue(Client1.DisconnectAsync());
            
            int setCount = 10000;
            
            for (int i = 0; i < setCount/2; i++)
            {
                Database2.Modify<int>(id, value => value + 1);
            }

            //reconnect client 1
            Assert.IsTrue(Client1.ConnectAsync());
            
            for (int i = 0; i < setCount/2; i++)
            {
                Database3.Modify<int>(id, value => value + 1);
            }
            
            TestUtility.AreEqual(setCount, () => Database1.Get<int>(id));
            TestUtility.AreEqual(setCount, () => Database2.Get<int>(id));
            TestUtility.AreEqual(setCount, () => Database3.Get<int>(id));
        }

        [Test]
        public static void TestRequestSuccess()
        {
            int modifyCount = 10000;
            
            string id = nameof(TestRequestSuccess);
            Setup(id);
            
            Server.AddCallback<SetValueRequest>(((message, session) =>
            {
                // -1 because the local mod count was already incremented by the previous SetValueRequest callback
                uint expected = Server.GetModCount(message.DatabaseId, message.ValueId) - 1;
                uint received = message.ModCount;
                
                Assert.AreEqual(expected, received);
            }));

            for (int i = 0; i < modifyCount; i++)
            {
                Database1.Modify<int>(id, value => value + 1);
            }
            
            TestUtility.AreEqual(modifyCount, () => Database1.Get<int>(id));
            TestUtility.AreEqual(modifyCount, () => Database2.Get<int>(id));
            TestUtility.AreEqual(modifyCount, () => Database3.Get<int>(id));
        }

        [Test]
        public static void TestOnModifyConfirm()
        {
            int modCount = 10000;
            
            string id = nameof(TestOnModifyConfirm);
            Setup(id);

            List<int> confirmedValues = new List<int>(modCount * 3);

            for (int i = 0; i < modCount; i++)
            {
                Database1.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                });
                Database2.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                });
                Database3.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                });
            }
            
            TestUtility.AreEqual(modCount * 3, () => Database1.Get<int>(id), timeInMs: 5000);
            TestUtility.AreEqual(modCount * 3, () => Database2.Get<int>(id));
            TestUtility.AreEqual(modCount * 3, () => Database3.Get<int>(id));
        }

        [Test]
        public static void TestClientHostPersistence()
        {
            string id = nameof(TestClientHostPersistence);
            Setup(id);
            
            TestUtility.AreEqual(1, () =>
            {
                int hostCount = 0;
                if (Database1.IsHost) hostCount++;
                if (Database2.IsHost) hostCount++;
                if (Database3.IsHost) hostCount++;
                return hostCount;
            }, "Exactly one host", 5000);

            Assert.IsTrue(Database1.IsHost);
            Assert.IsFalse(Database2.IsHost);
            Assert.IsFalse(Database3.IsHost);
            
            Database1.ClientPersistence = true;

            TestUtility.AreEqual(2, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            });

            Database1.HostPersistence = true;
            
            TestUtility.AreEqual(3, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            });

            Database1.HostPersistence = false;
            Database1.ClientPersistence = false;
            
            TestUtility.AreEqual(0, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            });
        }

        [Test]
        public static void TestHostId()
        {
            Setup(nameof(TestHostId));

            Database1.AddCallback<Guid>("HostId", guid => Console.WriteLine($"Set hostId to: {guid}"));
            
            Thread.Sleep(1000);
            
            Assert.IsTrue(Database1.IsHost);
            Assert.IsFalse(Database2.IsHost);
            Assert.IsFalse(Database3.IsHost);
        }

        [Test]
        public static void TestDelayedSynchronisation()
        {
            string id = nameof(TestDelayedSynchronisation);
            Setup(id);
            
            //disconnect client 1
            Client1.DisconnectAsync();
            TestUtility.AreEqual(false, (() => Client1.IsConnected));
            Thread.Sleep(1000);
            
            //set values in database 1
            Database1.Set("1", "1");
            Database1.Modify<string>("2", (current) => "2");
            
            TestUtility.AreEqual(0, () => Database1.Scheduler.QueuedTasksCount, "Processed sets!");
            
            //reconnect client 1
            Client1.ConnectAsync();
            Assert.IsTrue(Client1.WaitForConnect());
            
            TestUtility.AreEqual("1", () => Database1.Get<string>("1"));
            TestUtility.AreEqual("1", () => Database2.Get<string>("1"));
            TestUtility.AreEqual("1", () => Database3.Get<string>("1"));
            
            TestUtility.AreEqual("2", () => Database1.Get<string>("2"));
            TestUtility.AreEqual("2", () => Database2.Get<string>("2"));
            TestUtility.AreEqual("2", () => Database3.Get<string>("2"));
        }
    }
}