using System;

namespace Aksl.Concurrency
{
    public struct DisposeAction : IDisposable
    {
        private bool _isDisposed;
        private Action _action;

        public DisposeAction(Action action)

        {
            _isDisposed = false;
            _action = action;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _action();
        }
    }
}

