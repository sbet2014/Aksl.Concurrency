// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aksl.Concurrency
{
    /// <summary>
    /// Queue for executing asynchronous tasks in a first-in-first-out fashion.
    /// </summary>
    public class ActionBlock : IDisposable
    {
        AsyncLock _theLock;

        public ActionBlock()
        {
            _theLock = new AsyncLock();
        }

        public async Task Post(Func<Task> action, CancellationToken cancellationToken)
        {
            using (await _theLock.LockAsync(cancellationToken))
            {
                await action();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theLock.Dispose();
            }
        }
    }
}
