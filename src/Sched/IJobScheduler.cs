using System;

namespace Sched
{
    public interface IJobScheduler<in TJob>
    {
        /// <summary>
        /// Starts scheduling jobs
        /// This method is threadsafe.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops a running JobScheduler.
        /// Attemps to cancel running workers and waits for all workers to complete current jobs.
        /// This method is threadsafe.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stops a running JobScheduler.
        /// Attempts to cancel running works and waits for all workers to complete current jobs.
        /// This method is threadsafe.
        /// </summary>
        /// <param name="waitDuration">Maximum time to wait for worker</param>
        /// <returns>True if all workers completed within the waitDuration, otherwise false.</returns>
        bool Stop(TimeSpan waitDuration);

        /// <summary>
        /// Schedules a job. This method is threadsafe. Can be called before calling Start.
        /// </summary>
        /// <param name="job"></param>
        void ScheduleJob(TJob job);
    }
}