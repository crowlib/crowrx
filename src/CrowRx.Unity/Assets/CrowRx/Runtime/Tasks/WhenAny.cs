using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace CrowRx.Tasks
{
    using Internal;


    public partial struct CrowTask
    {
        /// <summary>첫 완료의 인덱스 또는 실패를 반환하고 모든 입력 작업의 완료를 소비합니다. 나머지 작업을 취소하지 않습니다. 반환 작업은 한 번만 await합니다.</summary>
        public static UniTask<int> WhenAny(UniTask[] tasks, int tasksLength) => WhenAnyPromise.Create(tasks, tasksLength);
    }

    internal sealed class WhenAnyPromise : IUniTaskSource<int>
    {
        private const int MaxCached = 64;
        private static readonly object PoolGate = new();
        private static WhenAnyPromise _poolHead;
        private static int _poolCount;
        private WhenAnyPromise _next;
        private int _remaining;
        private int _winnerClaimed;
        private int _consumptionState;
        private UniTaskCompletionSourceCore<int> _core;


        public static UniTask<int> Create(UniTask[] tasks, int tasksLength)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasksLength <= 0 || tasksLength > tasks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(tasksLength));
            }

            WhenAnyPromise promise;
            lock (PoolGate)
            {
                promise = _poolHead;
                if (promise is not null)
                {
                    _poolHead = promise._next;
                    promise._next = null;
                    _poolCount--;
                }
            }
            promise ??= new WhenAnyPromise();

            // 등록 루프와 모든 입력 완료, 호출자의 GetResult가 모두 끝난 뒤에만 재사용합니다.
            promise._remaining = tasksLength + 2;
            promise._winnerClaimed = 0;
            promise._consumptionState = 0;
            TaskTracker.TrackActiveTask(promise, 3);

            UniTask<int> task = new(promise, promise._core.Version);

            for (int i = 0; i < tasksLength; i++)
            {
                UniTask.Awaiter awaiter;
                try
                {
                    awaiter = tasks[i].GetAwaiter();
                }
                catch (Exception ex)
                {
                    if (Interlocked.CompareExchange(ref promise._winnerClaimed, 1, 0) == 0)
                    {
                        promise._core.TrySetException(ex);
                    }

                    promise.ChildFinished();
                    continue; // consume others.
                }

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuation(promise, awaiter, i);
                }
                else
                {
                    awaiter.SourceOnCompleted(
                        static state =>
                        {
                            using StateTuple<WhenAnyPromise, UniTask.Awaiter, int> t = (StateTuple<WhenAnyPromise, UniTask.Awaiter, int>)state;

                            TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                        },
                        StateTuple.Create(promise, awaiter, i));
                }
            }

            promise.ChildFinished();

            return task;
        }

        private static void TryInvokeContinuation(WhenAnyPromise self, in UniTask.Awaiter awaiter, int i)
        {
            Exception failure = null;

            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            try
            {
                if (Interlocked.CompareExchange(ref self._winnerClaimed, 1, 0) == 0)
                {
                    if (failure is null)
                    {
                        self._core.TrySetResult(i);
                    }
                    else
                    {
                        self._core.TrySetException(failure);
                    }
                }
            }
            finally
            {
                self.ChildFinished();
            }
        }

        private void ChildFinished()
        {
            if (Interlocked.Decrement(ref _remaining) == 0)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            // 승자만 소비하고 재사용하면 뒤늦은 입력 콜백이 새 작업을 오염시킵니다.
            // 단일 카운터를 마지막으로 감소시킨 경로만 반환하므로 재사용 중 중복 반환도 막습니다.
            _core.Reset();

            lock (PoolGate)
            {
                if (_poolCount >= MaxCached)
                {
                    return;
                }

                _next = _poolHead;
                _poolHead = this;
                _poolCount++;
            }
        }

        public int GetResult(short token)
        {
            if (_core.GetStatus(token) == UniTaskStatus.Pending)
            {
                throw new InvalidOperationException("The task has not completed.");
            }

            if (Interlocked.CompareExchange(ref _consumptionState, 1, 0) != 0)
            {
                throw new InvalidOperationException("WhenAny must be consumed only once.");
            }

            try
            {
                return _core.GetResult(token);
            }
            finally
            {
                TaskTracker.RemoveTracking(this);
                ChildFinished();
            }
        }

        public UniTaskStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object> continuation, object state, short token) => _core.OnCompleted(continuation, state, token);
        public UniTaskStatus UnsafeGetStatus() => _core.UnsafeGetStatus();

        void IUniTaskSource.GetResult(short token) => GetResult(token);
    }
}
