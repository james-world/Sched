#Sched#

Sched is a very simple library to create a job manager to allocate jobs to a fixed pool of workers.

##Basic Usage##

####Step 1###
Create a type to represent a job. This can be any type. Here's a simple example that just holds a job number:

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

####Step 2###

Create a worker that will process jobs which implements `IWorker<TJob>` and it's single method:

    void ExecuteJob(TJob job, System.Threading.CancellationToken ct)

Here is a simple example that waits for up to one second and writes out the job number. It also respects the cancellation token which is signalled when the job scheduler is stopped:

    public class Worker : IWorker<Job>
    {
        public void ExecuteJob(Job job, System.Threading.CancellationToken ct)
        {
            Task.Delay(TimeSpan.FromSeconds(1), ct).Wait(ct);
            Console.WriteLine(job.JobNumber + " completed.");
        }
    }

####Step 3###

Create a factory that produces worker instances. This is just a `Func<IWorker<TJob>>`. Here's a simple example:

   Func<IWorker<Job>> workerFactory = () => new Worker();

###Step 4###

Create the job scheduler and start it. The constructor accepts your worker factory and a count of workers to create:

    var jobScheduler = new JobScheduler<Job>(workerFactory, 2);
    jobScheduler.Start();

###Step 5###
Schedule jobs:

    for (int i = 1; i <= 10; i++)
    {
        jobScheduler.ScheduleJob(new Job(i));
    }

###Step 6###
Stop the scheduler. There are two overloads, `void Stop()` and `bool Stop(TimeSpan waitDuration`). The first blocks until pending jobs are complete. The second will limit how long the scheduler waits for pending jobs to complete, and returns a flag indicating whether they did.

   jobScheduler.Stop();

There is a complete example project in the source code.
 