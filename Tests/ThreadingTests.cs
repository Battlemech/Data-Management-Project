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
        public static void TestConcurrentScheduler()
        {
            int waitingTasksCount = 10000;
            int waitTime = 3000;

            ConcurrentScheduler scheduler = new ConcurrentScheduler();
            Stopwatch stopwatch = Stopwatch.StartNew();

            //track started tasks
            List<Task> waitingTasks = new List<Task>(waitingTasksCount);

            //start tasks which wait, "blocking" other tasks
            for (int i = 0; i < waitingTasksCount; i++)
            {
                Task task = new Task((() =>
                {
                    Thread.Sleep(waitTime);
                }));
                task.Start(scheduler);
                waitingTasks.Add(task);
            }
            
            Console.WriteLine($"Started all waiting tasks after {stopwatch.ElapsedMilliseconds}ms");

            //append a task which is supposed to execute immediately
            Task executeNowTask = new Task((() =>
            {
                Console.WriteLine($"Executed task without delay after {stopwatch.ElapsedMilliseconds}ms");
            }));
            executeNowTask.Start(scheduler);
            
            //wait for waiting tasks to complete
            Assert.IsTrue(Task.WaitAll(waitingTasks.ToArray(), waitTime + 3000));
            Console.WriteLine($"All waiting tasks completed after {stopwatch.ElapsedMilliseconds}ms");

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine($"Idle threads: {scheduler.IdleThreadCount}");
            }
        }

        [Test]
        public static void TestConcurrentSchedulerPerformance()
        {
            const int taskCount = 10000;
            const int taskTime = 150;
            
            ConcurrentScheduler scheduler = new ConcurrentScheduler();
            List<Task> tasks = new List<Task>();
            List<double> taskStartTime = new List<double>();

            //init tasks
            for (int i = 0; i < taskCount; i++)
            {
                Task task = new Task((() =>
                {
                    Thread.Sleep(taskTime);
                }));
                tasks.Add(task);
            }
            
            //start tasks
            Stopwatch taskStartTimer = new Stopwatch();
            foreach (var task in tasks)
            {
                taskStartTimer.Restart();
                task.Start(scheduler);
                taskStartTimer.Stop();
                taskStartTime.Add(taskStartTimer.ElapsedMilliseconds);
            }
            
            //wait until tasks are done
            Task.WaitAll(tasks.ToArray());
            
            Console.WriteLine($"Idle threads: {scheduler.IdleThreadCount}. Threads/Task: {(float) scheduler.IdleThreadCount/ (float)taskCount}");
            Console.WriteLine($"Average task starting time: {taskStartTime.Average()}ms");
        }
    }
}