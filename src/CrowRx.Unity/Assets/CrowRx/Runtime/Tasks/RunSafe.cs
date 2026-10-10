using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CrowRx.Tasks
{
    public partial struct CrowTask
    {
        public static void RunSafe(Func<CancellationToken, UniTask> taskFunc, CancellationToken token, bool useCancelLogging = false) => RunSafeAsync(taskFunc, token, useCancelLogging).Forget();
        public static void RunSafe<T>(Func<CancellationToken, UniTask<T>> taskFunc, CancellationToken token, bool useCancelLogging = false) => RunSafeAsync(taskFunc, token, useCancelLogging).Forget();

        private static async UniTask RunSafeAsync(Func<CancellationToken, UniTask> taskFunc, CancellationToken token, bool useCancelLogging)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                await taskFunc(token);
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    Log.Info($"[{nameof(RunSafe)}] Cancelled");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private static async UniTask<T> RunSafeAsync<T>(Func<CancellationToken, UniTask<T>> taskFunc, CancellationToken token, bool useCancelLogging)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                return await taskFunc(token);
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    Log.Info($"[{nameof(RunSafe)}<{typeof(T).Name}>] Cancelled");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            return default;
        }
    }
}