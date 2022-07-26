﻿using System;
using System.Threading.Tasks;
using DMP.Threading;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        private readonly QueuedScheduler _scheduler = new QueuedScheduler();
        
        public override int GetQueuedTasksCount()
        {
            return _scheduler.QueuedTasksCount;
        }
        
        public override void Delegate(Task task) => task.Start(_scheduler);

        public void Delegate(Action action) => Delegate(new Task(action));
    }
}