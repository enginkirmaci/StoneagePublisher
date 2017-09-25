using System;
using StoneagePublisher.ClassLibrary.Services;

namespace StoneagePublisher.Logging
{
    public class WindowLogger : ILogService
    {
        private readonly Action<string> logInfo;
        private readonly Action<string> logDebug;
        private readonly Action<string> logWarn;
        private readonly Action<string> logError;
        private readonly Action<string, Exception> logErrorException;
        private readonly Action<int> progress;

        public WindowLogger(
            Action<string> logInfo,
            Action<string> logDebug,
            Action<string> logWarn,
            Action<string> logError,
            Action<string, Exception> logErrorException,
            Action<int> progress)
        {
            this.logInfo = logInfo;
            this.logDebug = logDebug;
            this.logWarn = logWarn;
            this.logError = logError;
            this.logErrorException = logErrorException;
            this.progress = progress;
        }

        public void Info(string message)
        {
            logInfo?.Invoke(message);
        }

        public void Debug(string message)
        {
            logDebug?.Invoke(message);
        }

        public void Warn(string message)
        {
            logWarn?.Invoke(message);
        }

        public void Error(string message)
        {
            logError?.Invoke(message);
        }

        public void Error(string message, Exception exception)
        {
            logErrorException?.Invoke(message, exception);
        }

        public void Progress(int percentage)
        {
            progress?.Invoke(percentage);
        }
    }
}