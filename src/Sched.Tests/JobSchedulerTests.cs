using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sched.Tests
{
    public class JobSchedulerTests
    {
        private readonly List<TestWorker> _workers = new List<TestWorker>();

        private Func<IWorker<TestJob>> _workerFactory;

        [SetUp]
        public void SetUp()
        {
            _workerFactory = () =>
            {
                var worker = new TestWorker();
                _workers.Add(worker);
                return worker;
            };
        }

        [Test]
        public void ScheduledJobIsAssignedToWorker()
        {
            var sut = new JobScheduler<TestJob>(_workerFactory, 1);
            var job = new TestJob();

            sut.ScheduleJob(job);
            sut.Start();

            SpinWait.SpinUntil(() => job.Assigned, TimeSpan.FromSeconds(5));

            Assert.True(job.Assigned);
        }

        [Test]
        public void ScheduledJobIsCompletedByWorker()
        {
            var sut = new JobScheduler<TestJob>(_workerFactory, 1);
            var job = new TestJob();

            sut.ScheduleJob(job);
            sut.Start();

            job.MarkJobForCompletion();

            SpinWait.SpinUntil(() => job.Completed, TimeSpan.FromSeconds(5));

            Assert.True(job.Completed);
        }


        [Test]
        public void HonoursMaxConcurrency()
        {
            var sut = new JobScheduler<TestJob>(_workerFactory, 2);

            var job1 = new TestJob();
            var job2 = new TestJob();
            var job3 = new TestJob();

            sut.ScheduleJob(job1);
            sut.ScheduleJob(job2);
            sut.ScheduleJob(job3);

            sut.Start();

            SpinWait.SpinUntil(() => job1.Assigned, TimeSpan.FromSeconds(5));
            SpinWait.SpinUntil(() => job2.Assigned, TimeSpan.FromSeconds(5));           
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            Assert.False(job3.Assigned);
            Assert.AreEqual(2, _workers.Count);
        }

        [Test]
        public void StopCancelsPendingJobs()
        {
            _workerFactory = () =>
            {
                var worker = new TestWorker(doNotCancel: true);
                _workers.Add(worker);
                return worker;
            };
            var sut = new JobScheduler<TestJob>(_workerFactory, 2);

            var job1 = new TestJob();
            var job2 = new TestJob();
            var job3 = new TestJob();

            sut.ScheduleJob(job1);
            sut.ScheduleJob(job2);
            sut.ScheduleJob(job3);

            sut.Start();

            SpinWait.SpinUntil(() => job1.Assigned, TimeSpan.FromSeconds(5));
            SpinWait.SpinUntil(() => job2.Assigned, TimeSpan.FromSeconds(5));

            var completed = sut.Stop(TimeSpan.FromSeconds(1));

            Assert.False(completed);
        }

        [Test]
        public void StopDoesntWaitForPendingJobs()
        {
            var sut = new JobScheduler<TestJob>(_workerFactory, 2);

            var job1 = new TestJob();
            var job2 = new TestJob();
            var job3 = new TestJob();

            sut.ScheduleJob(job1);
            sut.ScheduleJob(job2);
            sut.ScheduleJob(job3);

            sut.Start();

            SpinWait.SpinUntil(() => job1.Assigned, TimeSpan.FromSeconds(5));
            SpinWait.SpinUntil(() => job2.Assigned, TimeSpan.FromSeconds(5));

            sut.Stop();

            Assert.IsEmpty(_workers[0].CompletedJobs());
            Assert.IsEmpty(_workers[1].CompletedJobs());
            Assert.False(job1.Completed);
            Assert.False(job2.Completed);
            Assert.False(job3.Assigned);
        }
    }

    public class TestWorker : IWorker<TestJob>
    {
        private readonly ConcurrentBag<TestJob> _completedJobs = new ConcurrentBag<TestJob>();
        private volatile TestJob _currentJob = null;
        private readonly bool _doNotCancel;

        public TestWorker(bool doNotCancel = false)
        {
            _doNotCancel = doNotCancel;
        }

        public void ExecuteJob(TestJob job, System.Threading.CancellationToken ct)
        {
            _currentJob = job;
            job.Assigned = true;
            try
            {
                var jobTask = job.WaitForJobCompletion();

                if (_doNotCancel)
                {
                    jobTask.Wait(TimeSpan.FromSeconds(10));
                }
                else
                {
                    jobTask.Wait(ct);
                }

                job.Completed = true;
            }
            catch (OperationCanceledException)
            {
            }

            _currentJob = null;
            if (job.Completed) _completedJobs.Add(job);
        }

        public TestJob CurrentJob()
        {
            return _currentJob;
        }

        public IList<TestJob> CompletedJobs()
        {
            return _completedJobs.ToArray();
        }
    }

    public class TestJob
    {
        private readonly TaskCompletionSource<int> _tcs = new TaskCompletionSource<int>();

        public bool Assigned { get; set; }

        public bool Completed { get; set; }

        public Task WaitForJobCompletion()
        {
            return _tcs.Task;
        }

        public void MarkJobForCompletion()
        {
            _tcs.SetResult(0);
        }
    }
}