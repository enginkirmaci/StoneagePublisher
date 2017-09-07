using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Handlers;
using System.Timers;
using StoneagePublisher.ClassLibrary.Entities;
using StoneagePublisher.ClassLibrary.Services;

namespace StoneagePublisher.Service.Watcher
{
    public class PublishWatcher
    {
        private readonly ILogService logService;

        //Miliseconds to wait to trigger a publish after a change in directory
        private const double TriggerWaitSeconds = 20000d;
        private const double PublishCheckFrequency = 5000d;
        private const int PercentagePrintFrequency = 5;
        private readonly ConfigurationProvider configurationProvider;
        private readonly DeploymentService deploymentService;

        private Dictionary<string, PublishWatchStatus> folderStatuses;
        
        private readonly Timer timer;
        private int lastPrintedPercentage = 0;

        public PublishWatcher(ILogService logService)
        {
            this.logService = logService;
            configurationProvider = new ConfigurationProvider();
            deploymentService = new DeploymentService(logService);
            deploymentService.ProgressChanged += DeploymentServiceOnProgressChanged;
            timer = new Timer
            {
                Interval = PublishCheckFrequency
            };

            timer.Elapsed += TimerOnElapsed;
        }

        private void DeploymentServiceOnProgressChanged(HttpProgressEventArgs args)
        {
            if (args.ProgressPercentage >= lastPrintedPercentage + PercentagePrintFrequency)
            {
                lastPrintedPercentage = args.ProgressPercentage;
                logService.Info($"Uploaded {args.ProgressPercentage}%");
            }
        }

        public void Initialize()
        {
            timer.Stop();

            folderStatuses = new Dictionary<string, PublishWatchStatus>();
            var config = configurationProvider.Getconfiguration();

            foreach (var profile in config.Profiles)
            {
                logService.Info($"Adding profile for path: {profile.LocalPublishFolder}");
                folderStatuses.Add(profile.LocalPublishFolder, new PublishWatchStatus());
                InitializeWatcher(profile.LocalPublishFolder);
            }

            timer.Enabled = true;
            timer.Start();
        }

        private void InitializeWatcher(string path)
        {
            if (!Directory.Exists(path))
            {
                logService.Warn($"Directory {path} does not exist, skipping.");
                return;
            }

            var watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.*"
            };

            watcher.Changed += WatcherOnChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            var profile = configurationProvider.Getconfiguration().Profiles.FirstOrDefault(x => fileSystemEventArgs.FullPath.StartsWith(x.LocalPublishFolder, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
            {
                logService.Error($"Could not find profile for path {fileSystemEventArgs.FullPath}");
                return;
            }
            
            var status = folderStatuses[profile.LocalPublishFolder];
            if (!status.LastUpdate.HasValue ||status.LastUpdate < DateTime.Now - TimeSpan.FromMilliseconds(TriggerWaitSeconds))
            {
                logService.Info($"Changes detected at path {profile.LocalPublishFolder}");
            }

            status.LastUpdate = DateTime.Now;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var publishTriggerTime = DateTime.Now - TimeSpan.FromMilliseconds(TriggerWaitSeconds);

            var configuration = configurationProvider.Getconfiguration();
            foreach (var profile in configuration.Profiles)
            {
                //logService.Info($"Checking changes for path {profile.LocalPublishFolder}");
                if (!folderStatuses.ContainsKey(profile.LocalPublishFolder))
                {
                    continue;
                }

                var status = folderStatuses[profile.LocalPublishFolder];

                if (!status.LastUpdate.HasValue)
                {
                    continue;
                }

                if (status.LastUpdate > publishTriggerTime)
                {
                    //logService.Info($"Waiting for more changes at path {profile.LocalPublishFolder}");
                    continue;
                }

                if (status.LastUpdate < publishTriggerTime && !status.ProcessedAfterPublish)
                {
                    status.LastProcessed = DateTime.Now;

                    lastPrintedPercentage = 0;
                    logService.Info($"Triggering deploy for profile at path {profile.LocalPublishFolder} Last Update: {status.LastUpdate}, LastProcessed: {status.LastProcessed ?? DateTime.MinValue}");
                    var deployed = deploymentService.CompressAndSend(profile.LocalPublishFolder, profile.RemotePublishFolder);
                    if (!deployed)
                    {
                        status.LastProcessed = null;
                    }
                }
            }
        }
    }
}
