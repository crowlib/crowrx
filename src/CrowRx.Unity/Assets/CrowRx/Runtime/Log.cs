// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;


namespace CrowRx.Utility
{
    public interface ILogger
    {
        void Info(string message);
        void Info(object message, UnityEngine.Object context);

        void Warning(string message);
        void Warning(object message, UnityEngine.Object context);

        void Error(string message);
        void Error(object message, UnityEngine.Object context);

        void Assertion(string message);
        void Assertion(string message, UnityEngine.Object context);

        void Exception(Exception exception);
        void Exception(Exception exception, UnityEngine.Object context);
    }

    public static class Log
    {
        private class DefaultLogger : ILogger
        {
            void ILogger.Info(string message) => UnityEngine.Debug.Log(message);
            void ILogger.Info(object message, UnityEngine.Object context) => UnityEngine.Debug.Log(message, context);

            void ILogger.Warning(string message) => UnityEngine.Debug.LogWarning(message);
            void ILogger.Warning(object message, UnityEngine.Object context) => UnityEngine.Debug.LogWarning(message, context);

            void ILogger.Error(string message) => UnityEngine.Debug.LogError(message);
            void ILogger.Error(object message, UnityEngine.Object context) => UnityEngine.Debug.LogError(message, context);

            void ILogger.Assertion(string message) => UnityEngine.Debug.LogAssertion(message);
            void ILogger.Assertion(string message, UnityEngine.Object context) => UnityEngine.Debug.LogAssertion(message, context);

            void ILogger.Exception(Exception exception) => UnityEngine.Debug.LogException(exception);
            void ILogger.Exception(Exception exception, UnityEngine.Object context) => UnityEngine.Debug.LogException(exception, context);
        }


        private static ILogger _logger = new DefaultLogger();


        public static void SetLogger(ILogger logger)
        {
            if (logger is null)
            {
                return;
            }

            _logger = logger;
        }

        [Conditional("CROWRX_LOG_INFO"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Info(string message) => _logger.Info(message);

        [Conditional("CROWRX_LOG_INFO"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Info(object message, UnityEngine.Object context) => _logger.Info(message, context);

        [Conditional("CROWRX_LOG_WARNING"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Warning(string message) => _logger.Warning(message);

        [Conditional("CROWRX_LOG_WARNING"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Warning(object message, UnityEngine.Object context) => _logger.Warning(message, context);

        [Conditional("CROWRX_LOG_ERROR"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Error(string message) => _logger.Error(message);

        [Conditional("CROWRX_LOG_ERROR"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Error(object message, UnityEngine.Object context) => _logger.Error(message, context);

        [Conditional("CROWRX_LOG_ASSERT"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Assertion(string message) => _logger.Assertion(message);

        [Conditional("CROWRX_LOG_ASSERT"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Assertion(string message, UnityEngine.Object context) => _logger.Assertion(message, context);

        [Conditional("CROWRX_LOG_EXCEPTION"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Exception(Exception exception) => _logger.Exception(exception);

        [Conditional("CROWRX_LOG_EXCEPTION"), Conditional("CROWRX_LOG_ALL"), Conditional("UNITY_EDITOR")]
        public static void Exception(Exception exception, UnityEngine.Object context) => _logger.Exception(exception, context);
    }
}