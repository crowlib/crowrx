using System.Threading;

namespace CrowRx
{
    public static class CancellationTokenSourceExtension
    {
        /// <summary>소유자가 취소 후 Dispose합니다. 취소 콜백이 실패해도 Dispose하며, 작업이 토큰 소스를 더 이상 사용하지 않을 때 호출합니다.</summary>
        public static void Release(this CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource is null)
            {
                return;
            }

            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
    }
}
