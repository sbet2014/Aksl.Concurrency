using System.Threading;
using System.Threading.Tasks;

namespace Aksl.Concurrency
{
    /// <summary>
    /// Notifies one or more waiting awaiters that an event has occurred
    /// 如果ManualResetEvent上有4个等待线程，当其中一个线程调用set()方法后，则4个等待线程都将完成
    /// </summary>
    public class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        /// <summary>
        /// Waits the async.
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            return _tcs.Task;
        }

        //public void Set() { m_tcs.TrySetResult(true); }
        /// <summary>
        /// Sets the state of the event to signaled, allowing one or more waiting awaiters to proceed.
        /// </summary>
        public void Set()
        {
            var tcs = _tcs;
            //Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
            //                      tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

            Task.Factory.StartNew((state) =>
            {
                ((TaskCompletionSource<bool>)state).TrySetResult(true);
            }, tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

            tcs.Task.Wait();
        }

        /// <summary>
        /// Sets the state of the event to nonsignaled, causing awaiters to block.
        /// </summary>
        public void Reset()
        {
            while (true)
            {
                var tcs = _tcs;
                //如果当前线程完成,重置
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                {
                    return;
                }
            }
        }
    }
}
