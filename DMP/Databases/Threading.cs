using System;
using System.Linq;
using System.Threading.Tasks;

namespace DMP.Databases
{
    public partial class Database
    {
        //Sum up all currently enqueued tasks
        public int QueuedTasksCount => _values.Values.Sum(vs => vs.GetQueuedTasksCount());
        
        public void Delegate(string id, Task task)
        {
            //enqueue task in value storage if it exists
            if (_values.TryGetValue(id, out ValueStorage.ValueStorage value)) value.Delegate(task);
            //start task if it doesn't exist
            else {task.Start();}
        }

        public void Delegate(string id, Action action) => Delegate(id, new Task(action));
    }
}