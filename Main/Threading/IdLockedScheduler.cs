using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Main.Threading
{
    public class IdLockedScheduler
    {
        private static readonly Random Rand = new Random();
        
        public int QueuedTasksCount => _queuedSchedulers.Values.Select(s => s.QueuedTasksCount).Sum();
        
        private readonly ConcurrentDictionary<string, QueuedScheduler> _queuedSchedulers =
            new ConcurrentDictionary<string, QueuedScheduler>();

        public void QueueTask(Task task)
        {
            //try to find an idle scheduler
            foreach (var queuedScheduler in _queuedSchedulers.Values)
            {
                if(queuedScheduler.ExecutingTasks) continue;
                
                //delegate task to idle scheduler
                task.Start(queuedScheduler);
                return;
            }

            DelegateToRandomScheduler(task);         
        }

        public void QueueTask(string id, Task task)
        {
            //delegate task to scheduler with specified id
            task.Start(GetScheduler(id));
        }

        private void DelegateToRandomScheduler(Task task)
        {
            int schedulerCount = _queuedSchedulers.Count;

            //create scheduler if necessary
            if (schedulerCount == 0)
            {
                _queuedSchedulers.TryAdd("", new QueuedScheduler());
            }
            
            //delegate task to random scheduler
            task.Start(_queuedSchedulers.Values.ElementAt(Rand.Next(0, schedulerCount)));
        }
        
        private QueuedScheduler GetScheduler(string id)
        {
            //if no scheduler for id exists
            if (!_queuedSchedulers.TryGetValue(id, out QueuedScheduler scheduler))
            {
                scheduler = new QueuedScheduler();
                
                //try adding new scheduler
                if (!_queuedSchedulers.TryAdd(id, scheduler))
                {
                    //scheduler with id exists. Try retrieving it again
                    return GetScheduler(id);
                }
            }

            return scheduler;
        }
    }
}