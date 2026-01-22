using System;
using Cysharp.Threading.Tasks;


namespace CrowRx.Tasks
{
    using Utility;


    public static partial class UniTaskExtension
    {
        public static void ForgetSafe(this UniTask task, bool useCancelLogging = false) => task.ForgetSafeAsync(useCancelLogging).Forget();
        public static void ForgetSafe<T>(this UniTask<T> task, bool useCancelLogging = false) => task.ForgetSafeAsync(useCancelLogging).Forget();


        private static async UniTask ForgetSafeAsync(this UniTask task, bool useCancelLogging)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    Log.Info($"[{nameof(ForgetSafe)}] Cancelled");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private static async UniTask<T> ForgetSafeAsync<T>(this UniTask<T> task, bool useCancelLogging)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                if (useCancelLogging)
                {
                    Log.Info($"[{nameof(ForgetSafeAsync)}<{typeof(T).Name}>] Cancelled");
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