using System;
using System.Threading.Tasks;
using DMP.Threading;

namespace DMP.Databases.ValueStorage
{
    public partial class ReadOnlyStorage<T>
    {
        private readonly QueuedScheduler _scheduler = new QueuedScheduler();
        
        public override int GetQueuedTasksCount()
        {
            return _scheduler.QueuedTasksCount;
        }
        
        public override Task Delegate(Task task)
        {
            task.Start(_scheduler);
            return task;
        }

        public Task Delegate(Action action) => Delegate(new Task(action));
    }
}