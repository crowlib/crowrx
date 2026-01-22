using System;
using Cysharp.Threading.Tasks;


namespace CrowRx.Tasks
{
    using Utility;


    public static partial class UniTaskExtension
    {
        public static async UniTask ContinueWithAnyway(this UniTask task, Action onEnd) =>
            await task.ContinueWith(
                onEnd,
                onEnd,
                _ => onEnd?.Invoke());

        public static async UniTask ContinueWithAnyway<T>(this UniTask<T> task, Action onEnd) =>
            await task.ContinueWith(
                _ => onEnd?.Invoke(),
                onEnd,
                _ => onEnd?.Invoke());

        public static async UniTask ContinueWithNotException(this UniTask task, Action onEnd) =>
            await task.ContinueWith(
                onEnd,
                onEnd,
                null);

        public static async UniTask ContinueWithNotException<T>(this UniTask<T> task, Action onEnd) =>
            await task.ContinueWith(
                _ => onEnd?.Invoke(),
                onEnd,
                null);

        public static async UniTask ContinueWith(this UniTask task, Action onSuccess, Action onCanceled, Action<Exception> onException)
        {
            try
            {
                await task;

                onSuccess?.Invoke();
            }
            catch (OperationCanceledException)
            {
                onCanceled?.Invoke();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);

                Log.Exception(ex);
            }
        }

        public static async UniTask ContinueWith<T>(this UniTask<T> task, Action<T> onSuccess, Action onCanceled, Action<Exception> onException)
        {
            try
            {
                onSuccess?.Invoke(await task);
            }
            catch (OperationCanceledException)
            {
                onCanceled?.Invoke();
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);

                Log.Exception(ex);
            }
        }
    }
}