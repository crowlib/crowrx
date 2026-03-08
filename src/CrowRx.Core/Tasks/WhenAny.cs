using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace CrowRx.Tasks
{
    using Internal;


    public partial struct CrowTask
    {
        public static UniTask<int> WhenAny(UniTask[] tasks, int tasksLength) => new(new WhenAnyPromise(tasks, tasksLength), 0);
    }

    internal sealed class WhenAnyPromise : IUniTaskSource<int>
    {
        private int _completedCount;
        private UniTaskCompletionSourceCore<int> _core;


        public WhenAnyPromise(UniTask[] tasks, int tasksLength)
        {
            if (tasksLength == 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            TaskTracker.TrackActiveTask(this, 3);

            for (int i = 0; i < tasksLength; i++)
            {
                UniTask.Awaiter awaiter;
                try
                {
                    awaiter = tasks[i].GetAwaiter();
                }
                catch (Exception ex)
                {
                    _core.TrySetException(ex);
                    continue; // consume others.
                }

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuation(this, awaiter, i);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        state =>
                        {
                            using StateTuple<WhenAnyPromise, UniTask.Awaiter, int> t = (StateTuple<WhenAnyPromise, UniTask.Awaiter, int>)state;

                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        },
                        StateTuple.Create(this, awaiter, i));
                }
            }
        }

        private static void TryInvokeContinuation(WhenAnyPromise self, in UniTask.Awaiter awaiter, int i)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self._core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self._completedCount) == 1)
            {
                self._core.TrySetResult(i);
            }
        }

        public int GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);

            return _core.GetResult(token);
        }

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);
        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        void IUniTaskSource.GetResult(short token) => GetResult(token);
    }
}