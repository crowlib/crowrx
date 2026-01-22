using System.Threading;


namespace CrowRx
{
    public static class CancellationTokenSourceExtension
    {
        public static void Release(this CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource is null)
            {
                return;
            }

            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
            
            cancellationTokenSource.Dispose();
        }
    }
}