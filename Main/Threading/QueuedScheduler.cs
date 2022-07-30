﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scheduler = System.Threading.Tasks.TaskScheduler;

namespace Main.Threading
{
    public class QueuedScheduler : Scheduler
    {
        public int QueuedTasksCount => _queuedTasks.Count;
        
        private readonly ConcurrentQueue<Task> _queuedTasks = new ConcurrentQueue<Task>();
        private bool _executingTasks = false;
        
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
                if(_executingTasks) return;
                _executingTasks = true;
            }
            
            DelegateTasksToThreadPool();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //executing tasks in line is too inefficient: It is disabled
            return false;
        }

        private void DelegateTasksToThreadPool()
        {
            Task.Run(Execute);
        }
        
        private void Execute()
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
                    _executingTasks = false;
                    return;
                }
            }
            
            //continue executing queued tasks
            Execute();
        }
    }
}