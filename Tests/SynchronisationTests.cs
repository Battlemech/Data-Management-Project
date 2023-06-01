using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DMP;
using DMP.Databases;
using DMP.Databases.Utility;
using DMP.Databases.ValueStorage;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Networking.Synchronisation.Server;
using DMP.Objects;
using DMP.Utility;
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

        [SetUp]
        public static void Setup()
        {
            int port = TestUtility.GetFreePort();
            
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
            Database2.Client = Client2;
            Database3.Client = Client3;

            Database1.IsSynchronised = true;
            Database2.IsSynchronised = true;
            Database3.IsSynchronised = true;
        }
        
        [TearDown]
        public static void TearDown()
        {
            Assert.IsTrue(Server.Stop());

            Client1.DisconnectAsync();
            Client2.DisconnectAsync();
            Client3.DisconnectAsync();

            Database1.Client = null;
            Database2.Client = null;
            Database3.Client = null;
        }

        [Test, Repeat(10)]
        public static void TestSetup()
        {
            
        }
        
        [Test]
        public static void TestSimpleSet()
        {
            string id = nameof(TestSimpleSet);
            
            //set value in database 1
            Database1.SetValue(id, id);
            
            //wait for synchronisation in databases 2 and 3
            TestUtility.AreEqual(id, (() => Database2.GetValue<string>(id)), "Test remote set after first get");
            TestUtility.AreEqual(id, (() => Database3.GetValue<string>(id)), "Test remote set before first get");
        }

        [Test]
        public static void TestListSet()
        {
            string id = nameof(TestListSet);
            
            //set value in database 1
            Database1.Add<List<int>, int>(id, 25);
            
            //wait for synchronisation in databases 2 and 3
            TestUtility.AreEqual(new List<int>(){25}, (() => Database2.GetValue<List<int>>(id)), "Test remote set after first get");
            TestUtility.AreEqual(new List<int>(){25}, (() => Database3.GetValue<List<int>>(id)), "Test remote set before first get");
        }

        [Test, Timeout(10000)]
        public static void TestConcurrentSets()
        {
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
                        Database1.SetValue(id, i);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 2");
                    
                    for (int i = 0; i < setCount; i++)
                    {
                        Database2.SetValue(id, -i);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    Console.WriteLine("Started task 3");
                    
                    for (int i = 0; i < setCount; i++)
                    {
                        Database3.SetValue(id, i * setCount);
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
                int a = Database1.GetValue<int>(id);
                int b = Database2.GetValue<int>(id);
                int c = Database3.GetValue<int>(id);
                return a == b && b == c;
            }), "values are equal", 10000 );
            
            //make sure all requests were processed
            TestUtility.AreEqual(0, (() => Database1.GetOngoingSets(id)));
            TestUtility.AreEqual(0, (() => Database2.GetOngoingSets(id)));
            TestUtility.AreEqual(0, (() => Database3.GetOngoingSets(id)));
            
            //check all values
            TestUtility.AreEqual(true, (() =>
            {
                int a = Database1.GetValue<int>(id);
                int b = Database2.GetValue<int>(id);
                int c = Database3.GetValue<int>(id);
                return a == b && b == c;
            }), "values are equal", 10000 );

            stopwatch.Stop();
            Console.WriteLine($"Synchronisation completed after {stopwatch.ElapsedMilliseconds} ms");
        }

        [Test]
        public static void TestConcurrentAdd()
        {
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
                TestUtility.IsChanging(0, () => database.QueuedTasksCount, "Internal tasks");
            }
            Console.WriteLine($"All adds completed internally after {stopwatch.ElapsedMilliseconds} ms");

            //make sure data was synchronised in all databases
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(true, (() => database.GetValue<List<int>>(id) != null), "List is not null");
                TestUtility.IsChanging(addCount * 3, () => database.GetValue<List<int>>(id).Count, "ElementCount");
                TestUtility.AreEqual(true, (() =>
                {
                    List<int> list = database.GetValue<List<int>>(id);
                    for (int i = 0; i < addCount * 3; i++)
                    {
                        if(list.Contains(i)) continue;
                    
                        Console.WriteLine($"List doesn't contain {i}");
                        return false;
                    }
                    return true;
                }));
                
                Assert.AreEqual(addCount * 3, database.GetValue<List<int>>(id).Count, "ElementCount - Late check");
            }
            
            stopwatch.Stop();
            Console.WriteLine($"All adds completed after: {stopwatch.ElapsedMilliseconds} ms");
            
            Console.WriteLine(LogWriter.StringifyCollection(Database1.GetValue<List<int>>(id)));
        }

        [Test]
        public static void TestSafeModifySimple()
        {
            string id = nameof(TestSafeModifySimple);
            
            Database1.SafeModify<int>(id, (value) => 100);
            
            TestUtility.AreEqual(100, () => Database1.GetValue<int>(id));
            TestUtility.AreEqual(100, () => Database2.GetValue<int>(id));
            TestUtility.AreEqual(100, () => Database3.GetValue<int>(id));
            
            Assert.AreEqual(1, Database1.GetModCount(id));
            Assert.AreEqual(1, Database2.GetModCount(id));
            Assert.AreEqual(1, Database3.GetModCount(id));
            
            Assert.AreEqual(0, Database1.GetOngoingSets(id));
            Assert.AreEqual(0, Database2.GetOngoingSets(id));
            Assert.AreEqual(0, Database3.GetOngoingSets(id));
        }
        
        [Test, Timeout(15000)]
        public static void TestSafeModify()
        {
            string id = nameof(TestSafeModify);
            
            //test options
            const int addCount = 1000;
            
            //track added values
            List<int> addedValues = new List<int>(addCount * 3);

            ModifyValueDelegate<List<int>> valueDelegate = value =>
            {
                //init list if necessary
                value ??= new List<int>();
                
                //get value to add
                int toAdd = value.Count;
                
                //add value locally
                Console.WriteLine($"Adding {toAdd}");
                value.Add(toAdd);
                            
                //make sure global copy doesn't contain new value
                Assert.IsFalse(addedValues.Contains(toAdd), $"{toAdd} was already added");
                addedValues.Add(toAdd);
                            
                return value;
            };

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Task[] tasks = new[]
            {
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database1.SafeModify(id, valueDelegate);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database2.SafeModify(id, valueDelegate);
                    }
                })),
                new Task((() =>
                {
                    resetEvent.WaitOne();
                    for (int i = 0; i < addCount; i++)
                    {
                        Database3.SafeModify(id, valueDelegate);
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
                TestUtility.AreEqual(0, (() => database.QueuedTasksCount), "Internal tasks", 5000);
            }
            Console.WriteLine($"All adds completed internally after {stopwatch.ElapsedMilliseconds} ms");
            
            //make sure data was synchronised in all databases
            foreach (var database in new Database[]{Database1, Database2, Database3})
            {
                TestUtility.AreEqual(addCount * 3, () => database.GetValue<List<int>>(id)?.Count, "ElementCount");
                TestUtility.AreEqual(true, (() =>
                {
                    List<int> list = database.GetValue<List<int>>(id);
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
            
            Console.WriteLine(LogWriter.StringifyCollection(Database1.GetValue<List<int>>(id)));

            TestUtility.AreEqual<uint>(addCount * 3, () => Database1.GetModCount(id));
            TestUtility.AreEqual<uint>(addCount * 3, () => Database2.GetModCount(id));
            TestUtility.AreEqual<uint>(addCount * 3, () => Database3.GetModCount(id));
            
            Assert.AreEqual(0, Database1.GetOngoingSets(id));
            Assert.AreEqual(0, Database2.GetOngoingSets(id));
            Assert.AreEqual(0, Database3.GetOngoingSets(id));
        }

        [Test]
        public static void TestSafeModifySync()
        {
            string id = nameof(TestSafeModifySync);

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

        [Test, Repeat(10)]
        public static void TestSimpleConnect()
        {
            string id = nameof(TestSimpleConnect);

            //disconnect client 1
            //todo: instead, wait until all requests were answered before disconnecting
            Thread.Sleep(1000); //wait until hostId of client was synchronised
            Assert.IsTrue(Client1.DisconnectAsync());
            
            //set value
            Database2.SetValue(id, id);
            
            //wait for modification
            TestUtility.AreEqual(id, () => Database3.GetValue<string>(id));
            Database3.Modify<string>(id, (value) =>
            {
                Console.WriteLine($"Database3: Modifying: {value}. Adding:4");
                return value + "4";
            });

            //wait for value to be synchronised in network
            TestUtility.AreEqual(id+"4", () => Database3.GetValue<string>(id), "Synchronisation before test");
            TestUtility.AreEqual(id+"4", () => Database2.GetValue<string>(id), "Synchronisation before test");
            TestUtility.AreEqual((uint) 2, () => Server.GetModCount(Database1.Id, id));
            
            //reconnect client 1
            Assert.IsTrue(Client1.ConnectAsync());
            Assert.IsTrue(Client1.WaitForConnect());
            
            //wait for value to be synchronised on previously disconnected client
            TestUtility.AreEqual(id+"4", () => Database1.GetValue<string>(id), "Synchronisation of value set before client connected");
            TestUtility.AreEqual((uint) 2, (() => Database1.GetModCount(id)));
            
            //try modifying result as newly connected client
            Database1.Modify<string>(id, (value) =>
            {
                Console.WriteLine($"Database 1: Updated value to {value}5");
                return value + "5";
            });
            
            TestUtility.AreEqual(id+"45", (() => Database1.GetValue<string>(id)));
            TestUtility.AreEqual(id+"45", (() => Database2.GetValue<string>(id)));
            TestUtility.AreEqual(id+"45", (() => Database3.GetValue<string>(id)));
            
            //make sure modCount was tracked correctly
            Assert.AreEqual(3, Database1.GetModCount(id));
            Assert.AreEqual(3, Database2.GetModCount(id));
            Assert.AreEqual(3, Database3.GetModCount(id));
            
            Console.WriteLine("-------Test successful-------");
        }

        [Test]
        public static void TestConnectDuringModification()
        {
            string id = nameof(TestConnectDuringModification);

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
            
            TestUtility.AreEqual(setCount, () => Database1.GetValue<int>(id));
            TestUtility.AreEqual(setCount, () => Database2.GetValue<int>(id));
            TestUtility.AreEqual(setCount, () => Database3.GetValue<int>(id));
        }

        [Test]
        public static void TestRequestSuccess()
        {
            int modifyCount = 10000;
            
            string id = nameof(TestRequestSuccess);

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
            
            TestUtility.AreEqual(modifyCount, () => Database1.GetValue<int>(id));
            TestUtility.AreEqual(modifyCount, () => Database2.GetValue<int>(id));
            TestUtility.AreEqual(modifyCount, () => Database3.GetValue<int>(id));
        }

        [Test]
        public static void TestOnModifyConfirm()
        {
            //todo: why does this test stop working at modCount: 10.000?
            int modCount = 10000;
            
            string id = nameof(TestOnModifyConfirm);

            List<int> confirmedValues = new List<int>(modCount * 3);

            for (int i = 0; i < modCount; i++)
            {
                Database1.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                    Console.WriteLine($"1-{confirmed}");
                });
                Database2.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                    Console.WriteLine($"2-{confirmed}");
                });
                Database3.Modify<int>(id, (value) => value + 1, confirmed =>
                {
                    Assert.IsFalse(confirmedValues.Contains(confirmed));
                    confirmedValues.Add(confirmed);
                    Console.WriteLine($"3-{confirmed}");
                });
            }
            
            TestUtility.AreEqual(modCount * 3, () => Database1.GetValue<int>(id));
            TestUtility.AreEqual(modCount * 3, () => Database2.GetValue<int>(id));
            TestUtility.AreEqual(modCount * 3, () => Database3.GetValue<int>(id));
        }

        [Test]
        public static void TestClientHostPersistence()
        {
            string id = nameof(TestClientHostPersistence);

            TestUtility.AreEqual(1, () =>
            {
                int hostCount = 0;
                if (Database1.IsHost) hostCount++;
                if (Database2.IsHost) hostCount++;
                if (Database3.IsHost) hostCount++;
                return hostCount;
            }, "Exactly one host", 5000);

            Database1.ClientPersistence.Set(true);
            Console.WriteLine($"Client 1 host: {Database1.IsHost}");
            Console.WriteLine($"Client 2 host: {Database2.IsHost}");
            Console.WriteLine($"Client 3 host: {Database3.IsHost}");

            TestUtility.AreEqual(true, (() => Database1.ClientPersistence), "Client Persistence synchronised");
            TestUtility.AreEqual(true, (() => Database2.ClientPersistence), "Client Persistence synchronised");
            TestUtility.AreEqual(true, (() => Database3.ClientPersistence), "Client Persistence synchronised");

            TestUtility.AreEqual(2, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            }, "All clients enabled persistence");

            Database1.HostPersistence.Set(true);
            
            TestUtility.AreEqual(3, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            }, "Client and host enabled persistence");

            Database1.HostPersistence.Set(false);
            Database1.ClientPersistence.Set(false);
            
            TestUtility.AreEqual(0, () =>
            {
                int persistenceCount = 0;
                if (Database1.IsPersistent) persistenceCount++;
                if (Database2.IsPersistent) persistenceCount++;
                if (Database3.IsPersistent) persistenceCount++;
                return persistenceCount;
            }, "Persistence was disabled globally");
        }

        [Test]
        public static void TestClientPersistence()
        {
            Console.WriteLine("User: Setting client persistence on Database1 to true");
            Database1.ClientPersistence.Set(true);

            //default persistence is false. Database will either be host or persistence
            TestUtility.AreEqual(true, () => Database1.IsHost || Database1.IsPersistent);
            TestUtility.AreEqual(true, () => Database2.IsHost || Database2.IsPersistent);
            TestUtility.AreEqual(true, () => Database3.IsHost || Database3.IsPersistent);
            
            //check again after pause to ensure continued persistence
            Thread.Sleep(3000);
            Assert.IsTrue(Database1.IsHost || Database1.IsPersistent);
            Assert.IsTrue(Database2.IsHost || Database2.IsPersistent);
            Assert.IsTrue(Database3.IsHost || Database3.IsPersistent);
        }

        [Test]
        public static void TestOnInitialized()
        {
            int invocationCount = 0;
            Database1.HostId.OnInitialized((guid =>
            {
                invocationCount++;
            }));
            
            Thread.Sleep(3000);
            
            Assert.AreEqual(1, invocationCount, "OnInitialized invoked exactly once");
        }

        [Test]
        public static void TestHostId()
        {
            Database1.AddCallback<Guid>("HostId", guid => Console.WriteLine($"Set hostId to: {guid}"));
            
            Thread.Sleep(1000);

            int hostCount = 0;
            if (Database1.IsHost) hostCount++;
            if (Database2.IsHost) hostCount++;
            if (Database3.IsHost) hostCount++;

            Assert.AreEqual(1, hostCount);
        }

        [Test]
        public static void TestSynchronisedObjectSynchronisation()
        {
            string testName = nameof(TestSynchronisedObjectSynchronisation);

            //make sure callbacks are triggered
            ManualResetEvent triggered1 = new ManualResetEvent(false);
            ManualResetEvent triggered2 = new ManualResetEvent(false);
            ManualResetEvent triggered3 = new ManualResetEvent(false);
            
            //allow waiting thread to proceed
            Database1.AddCallback<PlayerData>(testName, data =>
            {
                Console.WriteLine("Invoked callback 1");
                triggered1.Set();
            });
            Database2.AddCallback<PlayerData>(testName, data =>
            {
                Console.WriteLine("Invoked callback 2");
                triggered2.Set();
            });
            Database3.AddCallback<PlayerData>(testName, data =>
            {
                Console.WriteLine("Invoked callback 3");
                triggered3.Set();
            });
            
            //set value locally
            Database1.SetValue(testName, new PlayerData("PlayerData", "Jeff"));

            //ensure local synchronisation
            Assert.IsTrue(triggered1.WaitOne(Options.DefaultTimeout), "Local callback triggered");
            TestUtility.AreEqual("Jeff", () => Database1.GetValue<PlayerData>(testName)?.Name, "Local load");
            
            //ensure remote synchronisation
            Assert.IsTrue(triggered2.WaitOne(Options.DefaultTimeout), "Remote callback triggered");
            TestUtility.AreEqual("Jeff", () => Database2.GetValue<PlayerData>(testName)?.Name, "Remote load");

            Assert.IsTrue(triggered3.WaitOne(Options.DefaultTimeout), "Remote callback triggered");
            TestUtility.AreEqual("Jeff", () => Database3.GetValue<PlayerData>(testName)?.Name, "Remote load");

        }
        
        private class PlayerData : SynchronisedObject
        {
            public ValueStorage<string> Name => GetDatabase().Get<string>(nameof(Name));

            public PlayerData(string databaseId, string name) : base(databaseId)
            {
                Name.Set(name);
            }
        }
        
        [Test]
        public static void TestSafeModifyLocalCallback()
        {
            string id = nameof(TestSafeModifyLocalCallback);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Database1.AddCallback<string>(id, s =>
            {
                Console.WriteLine($"New value: {s}");
                resetEvent.Set();
            });
            
            Database1.SafeModify<string>(id, value => "Test");
            
            Assert.IsTrue(resetEvent.WaitOne(3000));
        }

        [Test]
        public static void TestDelete()
        {
            string id = nameof(TestDelete);

            Database1.SetValue(id, "Test");
            
            TestUtility.AreEqual("Test", (() => Database1.GetValue<string>(id)));
            TestUtility.AreEqual("Test", (() => Database2.GetValue<string>(id)));
            TestUtility.AreEqual("Test", (() => Database3.GetValue<string>(id)));
            
            Database1.Delete();
            
            TestUtility.AreEqual(null, (() => Database1.GetValue<string>(id)));
            TestUtility.AreEqual(null, (() => Database2.GetValue<string>(id)));
            TestUtility.AreEqual(null, (() => Database3.GetValue<string>(id)));

        }

        [Test]
        public static void TestObjectConstructor()
        {
            string id = nameof(TestObjectConstructor);

            //init test object
            Database1.SetValue(id, new TestObject("Test"));

            //TestObject has InvokeCount 1: It was increased when TestObject was created
            TestUtility.AreEqual(1, () => Database1.GetValue<TestObject>(id)?.InvokeCount.Get(), "Creator initialisation");
            
            //Accessing values on each database -> Invoking their constructors
            SynchronisedClient.SetInstance(Client2); //simulate client where SynchronisedObject is retrieved to be client 2
            TestUtility.AreEqual(2, () => Database2.GetValue<TestObject>(id)?.InvokeCount.Get(), "Accessed by number two");
            
            SynchronisedClient.SetInstance(Client3); //simulate client where SynchronisedObject is retrieved to be client 3
            TestUtility.AreEqual(3, () => Database3.GetValue<TestObject>(id)?.InvokeCount.Get(), "Accessed by number three");
            
            SynchronisedClient.SetInstance(Client1);
            
            Console.WriteLine($"Invoke count: {Database3.GetValue<TestObject>(id)?.InvokeCount.Get()}");
        }
        
        private class TestObject : SynchronisedObject
        {
            public ValueStorage<int> InvokeCount => GetDatabase().Get<int>(nameof(InvokeCount));

            public TestObject(string id, bool isPersistent = false) : base(id, isPersistent)
            {
                
            }

            protected override void Constructor()
            {
                Console.WriteLine($"Invoked constructor on {GetDatabase().Client}.");
                InvokeCount.Modify((value => value + 1));
            }
        }

        [Test]
        public static async Task TestModifyTask()
        {
            string id = nameof(TestModifyTask);

            string result = await Database1.Get<string>(id).ModifyAsync((value => id));
            
            Assert.AreEqual(id, result);
        }

        [Test]
        public static void TestConcurrentModifyTask()
        {
            int count = 1000;
            
            string id = nameof(TestConcurrentModifyTask);

            //generate ids to modify
            List<string> taskIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                taskIds.Add(i.ToString());                
            }
            
            //setup timers
            Stopwatch iterativeWatch = new Stopwatch();
            Stopwatch concurrentWatch = new Stopwatch();
            
            //try synchronous modification without task
            iterativeWatch.Start();
            foreach (var taskId in taskIds)
            {
                Database1.Modify(taskId, value => taskId + "!", out string s);
                Assert.AreEqual(taskId + "!", s);
            }
            iterativeWatch.Stop();

            //try asynchronous modification
            concurrentWatch.Start();
            var whenAll = Task.WhenAll(taskIds.Select((s => Database1.ModifyAsync<string>(s, value => s))));
            
            //wait for tasks to complete
            Assert.IsTrue(whenAll.Wait(Options.DefaultTimeout * 2), "Tasks timed out!");
            concurrentWatch.Stop();
            
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i.ToString(), whenAll.Result[i]);
            }
            
            Console.WriteLine($"Count: {count}. Iterative: {(float)iterativeWatch.ElapsedMilliseconds / (float)count} ms/obj. Concurrent: {(float)concurrentWatch.ElapsedMilliseconds / (float)count} ms/obj");
        }

        [Test]
        public static async Task TestExceptionHandling()
        {
            string id = nameof(TestExceptionHandling);

            //raise exceptions on callbacks
            Database1.AddCallback<string>(id, (s => throw new NotImplementedException("Local")));

            //catch local exception
            try
            {
                await Database1.Get<string>(id).Set("Test");
            }
            catch (NotImplementedException e)
            {
                Assert.AreEqual("Local", e.Message);
            }

            /*
             * cause remote exceptions
             */
            
            //add callbacks causing exceptions
            Assert.AreEqual(1, Database1.RemoveCallbacks(id));
            Database2.AddCallback<string>(id, (s => throw new NotImplementedException("Remote 2")));
            Database3.AddCallback<string>(id, (s => throw new NotImplementedException("Remote 3")));
            
            //add callbacks allowing to make sure all callbacks are still triggered
            int invokeCount = 0;
            Database1.AddCallback<string>(id, s => invokeCount++);
            Database2.AddCallback<string>(id, s => invokeCount++);
            Database3.AddCallback<string>(id, s => invokeCount++);
            
            await Database1.Get<string>(id).Set("Test");
            
            TestUtility.AreEqual(3, () => invokeCount, "Invoked all callbacks even after exceptions occour");
        }
        
    }
}