using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sched
{
    /// <summary>
    /// Manages a fixed size pool of IWorker instances created by
    /// the supplied IWorkerFactory and schedules jobs.
    /// </summary>
    /// <typeparam name="TJob">The type used to represent a job</typeparam>
    public class JobScheduler<TJob>
    {
        private readonly int _numWorkers;
        private readonly Func<IWorker<TJob>> _workerFactory;
        private bool _started;
        private Task[] _workerTasks;
        private BlockingCollection<TJob> _jobs;
        private CancellationTokenSource _cts;

        /// <summary>
        /// Creates the JobScheduler
        /// </summary>
        /// <param name="workerFactory">A factory called to create IWorker instances</param>
        /// <param name="numWorkers">The number of workers to create.</param>
        public JobScheduler(Func<IWorker<TJob>> workerFactory, int numWorkers)
        {
            _workerFactory = workerFactory;
            _numWorkers = numWorkers;
            _jobs = new BlockingCollection<TJob>(new ConcurrentQueue<TJob>());
        }

        /// <summary>
        /// Starts scheduling jobs
        /// This method is not threadsafe.
        /// </summary>
        public void Start()
        {
            if(_started)
                throw new InvalidOperationException("Calling Start more than once is not allowed");

            _started = true;
            _workerTasks = new Task[_numWorkers];            
            _cts = new CancellationTokenSource();

            for (int i = 0; i < _numWorkers; i++)
            {
                var worker = _workerFactory();
                _workerTasks[i] = Task.Factory.StartNew(
                    () => RunWorker(worker), _cts.Token);
            }
        }

        private void RunWorker(IWorker<TJob> worker)
        {
            var cancellationToken = _cts.Token;

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var job = _jobs.Take(cancellationToken);
                    worker.ExecuteJob(job, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }    
        }

        /// <summary>
        /// Stops a running JobScheduler.
        /// Attemps to cancel running workers and waits for all workers to complete current jobs.
        /// This method is not threadsafe.
        /// </summary>
        public void Stop()
        {
            Stop(TimeSpan.MaxValue);
        }

        /// <summary>
        /// Stops a running JobScheduler.
        /// Attempts to cancel running works and waits for all workers to complete current jobs.
        /// This method is not threadsafe.
        /// </summary>
        /// <param name="waitDuration">Maximum time to wait for worker</param>
        /// <returns>True if all workers completed within the waitDuration, otherwise false.</returns>
        public bool Stop(TimeSpan waitDuration)
        {
            if (!_started)
                throw new InvalidOperationException("Calling Stop before Start is not allowed");

            _cts.Cancel();

            var allCompleted = Task.WaitAll(_workerTasks, waitDuration);

            return allCompleted;
        }

        /// <summary>
        /// Schedules a job. This method is threadsafe. Can be called before calling Start.
        /// </summary>
        /// <param name="job"></param>
        public void ScheduleJob(TJob job)
        {
            _jobs.Add(job);
        }
    }
}