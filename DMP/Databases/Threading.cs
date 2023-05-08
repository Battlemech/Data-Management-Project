using System;
using System.Linq;
using System.Threading.Tasks;
using DMP.Threading;

namespace DMP.Databases
{
    public partial class Database
    {
        //Sum up all currently enqueued tasks
        public int QueuedTasksCount => _values.Values.Sum(vs => vs.GetQueuedTasksCount());
        
        public Task Delegate(string id, Task task)
        {
            //enqueue task in value storage if it exists
            if (_values.TryGetValue(id, out ValueStorage.ValueStorage value)) return value.Delegate(task);
            
            //start task if it doesn't exist
            task.Start();
            return task;
        }

        public Task Delegate(string id, Action action) => Delegate(id, new Task(action));
    }
}