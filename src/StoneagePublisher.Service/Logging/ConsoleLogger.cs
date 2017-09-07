using System;
using StoneagePublisher.ClassLibrary.Services;

namespace StoneagePublisher.Service.Logging
{
    public class ConsoleLogger : ILogService
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Debug(string message)
        {
            Console.WriteLine(message);
        }

        public void Warn(string message)
        {
            Console.WriteLine($"WARN: {message}");
        }

        public void Error(string message)
        {
            Console.WriteLine($"ERROR: {message}");
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine($"ERROR: {message} {Environment.NewLine}{exception.Message}");
        }
    }
}
