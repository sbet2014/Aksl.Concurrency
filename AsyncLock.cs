using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aksl.Concurrency
{
    public sealed class AsyncLock : IDisposable
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken)
                                .ConfigureAwait(continueOnCapturedContext: false);

            return new DisposeAction(() => _semaphore.Release());
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
