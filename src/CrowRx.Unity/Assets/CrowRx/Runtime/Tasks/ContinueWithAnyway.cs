using System;
using Cysharp.Threading.Tasks;

namespace CrowRx.Tasks
{
    public static partial class UniTaskExtension
    {
        public static UniTask ContinueWithAnyway(this UniTask task, Action onEnd) => ContinueWithCompletion(task, onEnd, true);
        public static UniTask ContinueWithAnyway<T>(this UniTask<T> task, Action onEnd) => ContinueWithCompletion(task, onEnd, true);

        public static UniTask ContinueWithNotException(this UniTask task, Action onEnd) => ContinueWithCompletion(task, onEnd, false);
        public static UniTask ContinueWithNotException<T>(this UniTask<T> task, Action onEnd) => ContinueWithCompletion(task, onEnd, false);

        private static async UniTask ContinueWithCompletion(UniTask task, Action onEnd, bool invokeOnException)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                HandleCompletionFailure(onEnd, invokeOnException, exception);
                return;
            }

            onEnd?.Invoke();
        }

        private static async UniTask ContinueWithCompletion<T>(UniTask<T> task, Action onEnd, bool invokeOnException)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                HandleCompletionFailure(onEnd, invokeOnException, exception);
                return;
            }

            onEnd?.Invoke();
        }

        private static void HandleCompletionFailure(Action onEnd, bool invokeOnException, Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                onEnd?.Invoke();
            }
            else
            {
                if (invokeOnException)
                {
                    onEnd?.Invoke();
                }

                Log.Exception(exception);
            }
        }

        public static async UniTask ContinueWith(this UniTask task, Action onSuccess, Action onCanceled, Action<Exception> onException)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                onCanceled?.Invoke();
                return;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);

                Log.Exception(ex);
                return;
            }

            // 콜백의 예외를 원래 작업의 실패/취소로 다시 처리하지 않습니다.
            onSuccess?.Invoke();
        }

        public static async UniTask ContinueWith<T>(this UniTask<T> task, Action<T> onSuccess, Action onCanceled, Action<Exception> onException)
        {
            T result;

            try
            {
                result = await task;
            }
            catch (OperationCanceledException)
            {
                onCanceled?.Invoke();
                return;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);

                Log.Exception(ex);
                return;
            }

            onSuccess?.Invoke(result);
        }
    }
}
