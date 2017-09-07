using System;
using System.Threading;
using System.Threading.Tasks;
using StoneagePublisher.Service.Logging;
using StoneagePublisher.Service.Watcher;

namespace StoneagePublisher.Service
{
    class Program
    {
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            // kick off asynchronous stuff 

            Task.Run(() => {
                var logService = new ConsoleLogger();
                Console.WriteLine("Initialiazing watcher");
                var watcher = new PublishWatcher(logService);
                watcher.Initialize();
            });

            _quitEvent.WaitOne();

            // cleanup/shutdown and quit

        }
    }
}
