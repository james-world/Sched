using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sched.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<IWorker<Job>> jobFactory = () => new Worker();

            var jobScheduler = new JobScheduler<Job>(jobFactory, 2);

            for (int i = 1; i <= 10; i++)
            {
                jobScheduler.ScheduleJob(new Job(i));
            }

            jobScheduler.Start();

            Console.WriteLine("Press a key to terminate jobScheduler");
            Console.ReadKey(true);

            jobScheduler.Stop();

            Console.WriteLine("Done. Press a key to exit");
            Console.ReadKey(true);
        }
    }

    public class Job
    {
        private readonly int _jobNumber;

        public Job(int jobNumber)
        {
            _jobNumber = jobNumber;
        }

        public int JobNumber
        {
            get { return _jobNumber; }
        }
    }

    public class Worker : IWorker<Job>
    {
        public void ExecuteJob(Job job, System.Threading.CancellationToken ct)
        {
            Task.Delay(TimeSpan.FromSeconds(1), ct).Wait(ct);
            Console.WriteLine(job.JobNumber + " completed.");
        }
    }

}
