using UnityEngine;

namespace Julo.Logging
{
    public class Log
    {

        class DefaultLogger : Logger
        {
            public void Debug(string message, params object[] args) { UnityEngine.Debug.Log(System.String.Format(message, args)); }
            public void Info(string message, params object[] args) { UnityEngine.Debug.Log(System.String.Format(message, args)); }
            public void Warn(string message, params object[] args) { UnityEngine.Debug.LogWarning(System.String.Format(message, args)); }
            public void Error(string message, params object[] args) { UnityEngine.Debug.LogError(System.String.Format(message, args)); }
        }

        static Logger _logger;
        static Logger logger
        {
            get {
                if(_logger == null)
                {

                    _logger = new DefaultLogger();

                }
                return _logger;
            }
        }

        public static void SetLogger(Logger logger)
        {
            _logger = logger;
        }

        public static void Debug(string message, params object[] args) { logger.Debug(message, args); }
        public static void Info(string message, params object[] args) { logger.Info(message, args); }
        public static void Warn(string message, params object[] args) { logger.Warn(message, args); }
        public static void Error(string message, params object[] args) { logger.Error(message, args); }

    } // class Log

} // namespace Julo.Logging
