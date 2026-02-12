using System;
using System.Diagnostics;


namespace CrowRx
{
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Assertion(string message);
        void Exception(Exception exception);
    }

    public static class Log
    {
        private class DefaultLogger : ILogger
        {
            void ILogger.Info(string message) => Console.WriteLine(message);
            void ILogger.Warning(string message) => Console.Error.WriteLine($"Warning : {message}");
            void ILogger.Error(string message) => Console.Error.WriteLine($"Error : {message}");
            void ILogger.Assertion(string message) => Console.Error.WriteLine($"Assertion : {message}");
            void ILogger.Exception(Exception exception) => Console.Error.WriteLine($"Exception : {exception}");
        }


        private static ILogger _logger = new DefaultLogger();


        public static void SetLogger(ILogger? logger)
        {
            if (logger is null)
            {
                return;
            }

            _logger = logger;
        }

        [Conditional("CROWRX_LOG_INFO"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Info(string message) => _logger.Info(message);

        [Conditional("CROWRX_LOG_WARNING"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Warning(string message) => _logger.Warning(message);

        [Conditional("CROWRX_LOG_ERROR"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Error(string message) => _logger.Error(message);

        [Conditional("CROWRX_LOG_ASSERT"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Assertion(string message) => _logger.Assertion(message);

        [Conditional("CROWRX_LOG_EXCEPTION"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Exception(Exception exception) => _logger.Exception(exception);
    }
}