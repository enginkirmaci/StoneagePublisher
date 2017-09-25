using System;
using StoneagePublisher.ClassLibrary.Services;

namespace StoneagePublisher.Service.Logging
{
    public class ConsoleLogger : ILogService
    {
        private int lastPrintedPercentage = 0;
        private const int PercentagePrintFrequency = 5;

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

        public void Progress(int percentage)
        {
            if (percentage == 0 && lastPrintedPercentage == 100)
                lastPrintedPercentage = 0;

            if (percentage >= lastPrintedPercentage + PercentagePrintFrequency)
            {
                lastPrintedPercentage = percentage;
                Console.WriteLine($"Percentage: {percentage}%");
            }
        }
    }
}