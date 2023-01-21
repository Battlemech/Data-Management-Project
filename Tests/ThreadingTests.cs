using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DMP.Threading;
using NUnit.Framework;

namespace Tests
{
    public static class ThreadingTests
    {
        [Test]
        public static void TestQueuedScheduler()
        {
            int taskCount = 100000;
            List<Task> tasks = new List<Task>();
            QueuedScheduler scheduler = new QueuedScheduler();

            int lastAdd = -1;

            //start tasks
            for (int i = 0; i < taskCount; i++)
            {
                var i1 = i;
                Task task = new Task((() =>
                {
                    Console.WriteLine($"Executing task {i1}");

                    //make sure tasks are added in right order
                    lock (tasks)
                    {
                        Assert.AreEqual(lastAdd + 1, i1);
                        lastAdd = i1;
                    }
                }));
                
                tasks.Add(task);
                task.Start(scheduler);
            }
            
            //wait for them
            Task.WaitAll(tasks.ToArray());
        }

        [Test]
        public static void TestManyQueuedSchedulers()
        {
            int schedulerCount = 100;
            int taskCount = 10;
            int waitTime = 100;

            //track when tasks are done executing
            Stopwatch stopwatch = new Stopwatch();
            long firstEnd = long.MaxValue;
            long lastEnd = long.MinValue;
            
            //signal tasks when to start
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            
            //save created schedulers
            Dictionary<int, QueuedScheduler> schedulers = new Dictionary<int, QueuedScheduler>();
            
            //create tasks for each scheduler
            Dictionary<int, List<Task>> taskDistribution = new Dictionary<int, List<Task>>();
            for (int i = 0; i < schedulerCount; i++)
            {
                //create scheduler
                schedulers[i] = new QueuedScheduler();
                
                List<Task> tasks = new List<Task>();
                for (int j = 0; j < taskCount; j++)
                {
                    //save values for print
                    var i1 = i;
                    var j1 = j;
                    tasks.Add(new Task((() =>
                    {
                        //wait for tasks to start
                        resetEvent.WaitOne();
                        
                        Console.WriteLine($"Scheduler {i1} is executing task {j1}");
                        
                        Thread.Sleep(waitTime);
                        
                        long elapsedTime = stopwatch.ElapsedMilliseconds;

                        lock (taskDistribution)
                        {
                            if (firstEnd > elapsedTime) firstEnd = elapsedTime;
                            if (lastEnd < elapsedTime) lastEnd = elapsedTime;
                        }
                    })));
                }

                taskDistribution[i] = tasks;
            }
            

            //start tasks
            foreach (var kv in taskDistribution)
            {
                QueuedScheduler scheduler = schedulers[kv.Key];
                
                foreach (var task in kv.Value)
                {
                    task.Start(scheduler);    
                }
            }
            
            //start tracking time
            stopwatch.Start();

            //signal first tasks to start executing
            resetEvent.Set();
            
            //wait for all tasks to finish
            foreach (var tasks in taskDistribution.Values)
            {
                Task.WaitAll(tasks.ToArray());
            }
            
            //print test results
            Console.WriteLine($"Earliest done: {firstEnd} ms. Latest done: {lastEnd} ms. Variance: {lastEnd - firstEnd} ms");
        }
    }
}