using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DMP.Threading
{
    public class ConcurrentScheduler : TaskScheduler
    {
        public static ConcurrentScheduler Instance = new ConcurrentScheduler();
        
        private static Random _random = new Random();

        /// <summary>
        /// Amount of idle threads which won't be terminated automatically
        /// </summary>
        private readonly int _maxIdleThreads;
        
        /// <summary>
        /// Amount of time an idle helper thread will wait for new tasks before terminating
        /// </summary>
        private readonly int _idleTimeBeforeTermination;
        
        /// <summary>
        /// Amount of idle helper threads to terminate at the same time
        /// </summary>
        private readonly int _terminationCount;

        //tracks tasks to be executed
        private readonly ConcurrentQueue<Task> _toExecuteTasks = new ConcurrentQueue<Task>();
        
        //event signaling a single thread to start dequeuing tasks
        private readonly AutoResetEvent _taskAddedEvent = new AutoResetEvent(false);
        
        //track idle thread count
        public int IdleThreadCount => _idleThreadCount;
        private int _idleThreadCount = 0;
        
        //track active threads
        private int _threadCount = 0;

        public ConcurrentScheduler(int maxIdleThreads = 3, int idleTimeBeforeTermination = 100, int terminationCount = 2)
        {
            _maxIdleThreads = maxIdleThreads;
            _idleTimeBeforeTermination = idleTimeBeforeTermination;
            _terminationCount = terminationCount;
        }
        
        protected override void QueueTask(Task task)
        {
            _toExecuteTasks.Enqueue(task);

            //start thread which will execute tasks
            if (_idleThreadCount == 0)
            {
                //background threads terminate automatically, no need to track them
                Thread thread = new Thread(StartThread) { IsBackground = true };
                thread.Start();
            }
            else
            {
                //signal a waiting thread that new work was added
                _taskAddedEvent.Set();
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return !taskWasPreviouslyQueued && TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _toExecuteTasks;
        }

        private void StartThread() => ExecuteTasks(Interlocked.Increment(ref _threadCount));
        
        private void ExecuteTasks(int threadIndex)
        {
            while (true)
            {
                while (_toExecuteTasks.TryDequeue(out Task task))
                {
                    TryExecuteTask(task);
                }

                //signal that thread is now idle
                Interlocked.Increment(ref _idleThreadCount);
                
                //if thread was created to deal with onslaught of new tasks
                if (threadIndex >= _maxIdleThreads)
                {
                    /*
                     * Terminate newest created thread if no new work for thread is received within a short timespan
                     * to iteratively terminate idle threads
                     */
                    
                    
                    //wait for more work within a short timeframe, adding additional wait time proportional to queue position
                    while (!_taskAddedEvent.WaitOne(CalculateWaitTime(threadIndex)))
                    {
                        //if current thread isn't the newest created one: Try to wait again, checking queue position again
                        if (threadIndex + _terminationCount < _threadCount) continue;

                        //thread will be terminating and is no longer idle
                        Interlocked.Decrement(ref _idleThreadCount);
                        
                        //thread no longer exists
                        Interlocked.Decrement(ref _threadCount);

                        //terminate thread
                        return;
                    }
                }
                else
                {
                    //wait for more work
                    _taskAddedEvent.WaitOne();
                    Interlocked.Decrement(ref _idleThreadCount);   
                }
            }
        }

        private int CalculateWaitTime(int threadIndex)
        {
            int queuePosition = Math.Max(0, _threadCount - threadIndex - _terminationCount);
            int waitTime = (1 + queuePosition/_terminationCount) * (_idleTimeBeforeTermination + 10);

            return waitTime;
        }
    }
}