using System;
using System.Threading.Tasks;

namespace DMP.Threading
{
    public static class Delegation
    {
        /// <summary>
        /// Scheduler used for tasks which need to be executed at the same time
        /// </summary>
        public static readonly ConcurrentScheduler ConcurrentScheduler = new ConcurrentScheduler();

        /// <summary>
        /// Scheduler used for tasks with low priority which may be executed after another.
        /// Primarily used for tasks which need to wait
        /// </summary>
        public static readonly QueuedScheduler QueuedScheduler = new QueuedScheduler();

        /// <summary>
        /// Delegate an action, attempting to execute it as soon as possible
        /// </summary>
        public static void DelegateAction(Action action) => DelegateTask(new Task(action));
        /// <summary>
        /// Delegate a task, attempting to execute it as soon as possible
        /// </summary>
        public static void DelegateTask(Task task) => task.Start(ConcurrentScheduler);
        
        
        /// <summary>
        /// Enqueue an action, executing it after other previously enqueued actions
        /// </summary>
        public static void EnqueueAction(Action action) => EnqueueTask(new Task(action));
        /// <summary>
        /// Enqueue a task, executing it after other previously enqueued tasks
        /// </summary>
        public static void EnqueueTask(Task task) => task.Start(QueuedScheduler);


    }
}