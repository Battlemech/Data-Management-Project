using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Scheduler = System.Threading.Tasks.TaskScheduler;

namespace DMP.Threading
{
    public class QueuedScheduler : Scheduler
    {
        public static readonly QueuedScheduler Instance = new QueuedScheduler();
        public static void EnqueueTask(Task task) => Instance.Enqueue(task);
        public static void EnqueueAction(Action action) => Instance.Enqueue(action);
        
        public int QueuedTasksCount => _queuedTasks.Count + (ExecutingTasks ? 1 : 0);
        public bool ExecutingTasks { get; private set; }
        
        private readonly ConcurrentQueue<Task> _queuedTasks = new ConcurrentQueue<Task>();

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _queuedTasks;
        }
        
        protected override void QueueTask(Task task)
        {
            _queuedTasks.Enqueue(task);

            //start executing tasks if no thread is doing that already
            lock (_queuedTasks)
            {
                if(ExecutingTasks) return;
                ExecutingTasks = true;
            }

            //delegate task to thread pool
            Task.Run(Execute);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //executing tasks in line is too inefficient: It is disabled
            return false;
        }

        private void Execute()
        {
            while (true)
            {
                //execute queued tasks
                while (_queuedTasks.TryDequeue(out Task task))
                {
                    TryExecuteTask(task);
                }

                //try to stop executing tasks
                lock (_queuedTasks)
                {
                    //if no tasks are queued: stop executing
                    if (_queuedTasks.IsEmpty)
                    {
                        ExecutingTasks = false;
                        return;
                    }
                }

                //continue executing queued tasks
            }
        }
        
        public void Enqueue(Task task) => task.Start(this);
        public void Enqueue(Action action) => EnqueueTask(new Task(action));
    }
}