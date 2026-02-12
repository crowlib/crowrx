using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace CrowRx.Tasks
{
    using Utility;


    public partial struct CrowTask
    {
        public static void RunSafe(Func<UniTask> taskFunc, CancellationToken token, bool useCancelLogging = false) => RunSafeAsync(taskFunc, token, useCancelLogging).Forget();
        public static void RunSafe<T>(Func<UniTask<T>> taskFunc, CancellationToken token, bool useCancelLogging = false) => RunSafeAsync(taskFunc, token, useCancelLogging).Forget();

        private static async UniTask RunSafeAsync(Func<UniTask> taskFunc, CancellationToken token, bool useCancelLogging)
        {
            try
            {
                await taskFunc().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    UnityLog.Info($"[{nameof(RunSafe)}] Cancelled");
                }
            }
            catch (Exception ex)
            {
                UnityLog.Exception(ex);
            }
        }

        private static async UniTask<T> RunSafeAsync<T>(Func<UniTask<T>> taskFunc, CancellationToken token, bool useCancelLogging)
        {
            try
            {
                return await taskFunc().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    UnityLog.Info($"[{nameof(RunSafe)}<{typeof(T).Name}>] Cancelled");
                }
            }
            catch (Exception ex)
            {
                UnityLog.Exception(ex);
            }

            return default;
        }
    }
}