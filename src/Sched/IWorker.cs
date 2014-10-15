using System.Threading;

namespace Sched
{
    public interface IWorker<in TJob>
    {
        void ExecuteJob(TJob job, CancellationToken ct);
    }
}
