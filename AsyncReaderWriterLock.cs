using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aksl.Concurrency
{
    public class AsyncReaderWriterLock
    {
        private readonly Task<DisposeAction> _readerReleaser;
        private readonly Task<DisposeAction> _writerReleaser;

        private readonly Queue<TaskCompletionSource<DisposeAction>> _waitingWriters = new Queue<TaskCompletionSource<DisposeAction>>();
        private TaskCompletionSource<DisposeAction> _waitingReader = new TaskCompletionSource<DisposeAction>();
        private int _readersWaiting;

        private int _lockStatus; // -1 means write lock, >=0 no. of read locks

        public AsyncReaderWriterLock()
        {
            _readerReleaser = Task.FromResult(new DisposeAction(this.ReaderRelease));
            _writerReleaser = Task.FromResult(new DisposeAction(this.WriterRelease));
        }

        public Task<DisposeAction> ReaderLockAsync()
        {
            lock (_waitingWriters)
            {
                bool hasPendingReaders = _lockStatus >= 0;
                bool hasNoPendingWritiers = _waitingWriters.Count == 0;
                if (hasPendingReaders && hasNoPendingWritiers) //如果只有读出者在等待，无写入者等待。意味着允许有多个读出者同时读出。
                {
                    ++_lockStatus;//读状态
                    return _readerReleaser;
                }
                else
                {
                    ++_readersWaiting;//等待读出者计数
                    return _waitingReader.Task.ContinueWith(t => t.Result);//阻塞读出者
                }
            }
        }

        public Task<DisposeAction> WriterLockAsync()
        {
            lock (_waitingWriters)
            {
                bool hasNoPendingReaders = _lockStatus == 0;
                if (hasNoPendingReaders) //锁空闲
                {
                    _lockStatus = -1;//写标志
                    return _writerReleaser;
                }
                else
                {
                    var waiter = new TaskCompletionSource<DisposeAction>();
                    _waitingWriters.Enqueue(waiter); //进队列
                    return waiter.Task; //阻塞写入者
                }
            }
        }

        private void ReaderRelease()
        {
            TaskCompletionSource<DisposeAction> toWake = null;

            lock (_waitingWriters)
            {
                --_lockStatus;//递减
                if (_lockStatus == 0 && _waitingWriters.Count > 0) //直到读出者全部读完,这时如有等待写入者,唤醒等待写入者
                {
                    _lockStatus = -1;//写标志
                    toWake = _waitingWriters.Dequeue(); //唤醒等待写入者
                }
            }

            if (toWake != null)
            {
                toWake.SetResult(new DisposeAction(this.WriterRelease));
            }
        }

        private void WriterRelease()
        {
            TaskCompletionSource<DisposeAction> toWake = null;
            Action wakeupAction = this.ReaderRelease;

            lock (_waitingWriters)
            {
                if (_waitingWriters.Count > 0)//如果有等待写入者，优先写入,直到无等待写入者
                {
                    toWake = _waitingWriters.Dequeue();//写入者设置为唤醒
                    wakeupAction = this.WriterRelease;
                }
                else if (_readersWaiting > 0)//如果有等待读出者，
                {
                    toWake = _waitingReader;//读出者设置为唤醒,这时已经没有等待的写入者，所以会同时读出
                    _lockStatus = _readersWaiting;
                    //复位
                    _readersWaiting = 0;
                    _waitingReader = new TaskCompletionSource<DisposeAction>();
                }
                else
                {
                    _lockStatus = 0;
                }
            }

            if (toWake != null)
            {
                toWake.SetResult(new DisposeAction(wakeupAction));
            }
        }
    }
}
