using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using StoneagePublisher.ClassLibrary;
using StoneagePublisher.ClassLibrary.Command;
using StoneagePublisher.ClassLibrary.Entities;
using StoneagePublisher.ClassLibrary.Services;
using StoneagePublisher.Logging;
using StoneagePublisher.Service.Watcher;

namespace StoneagePublisher.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DeploymentService deploymentService;
        private readonly WindowLogger windowLogger;
        private readonly CompressionService compressionService;
        private PublishWatcher publishWatcher;
        private Profile _selectedProfile;
        private bool _canExecute;
        private ICommand _zipSendCommand;
        private ICommand _autoModeCommand;

        public Profile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                _selectedProfile = value;
                RaisePropertyChanged("SelectedProfile");
            }
        }

        public Configuration Configuration { get; set; }

        public IList<Profile> Profiles { get; set; }
        public string Status { get; set; }

        public ICommand ZipSendCommand => _zipSendCommand ?? (_zipSendCommand = new CommandHandler(() => ZipSend(), _canExecute));

        public ICommand AutoModeCommand => _autoModeCommand ?? (_autoModeCommand = new CommandHandler(() =>
        {
            IsAutoMode = IsAutoMode ? false : true;
            InitializeWatcher();
            Status = string.Empty;
            RaisePropertyChanged("Status");
            RaisePropertyChanged("IsAutoMode");
        }, _canExecute));

        public bool IsAutoMode { get; set; }

        public int Progress { get; set; }

        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Status = "Status";
                Progress = 35;
                Configuration = new Configuration()
                {
                    Profiles = new List<Profile>()
                    {
                        new Profile()
                        {
                            LocalPublishFolder="LocalPublishFolder",
                            Name="Name",
                            RemotePublishFolder="RemotePublishFolder"
                        },
                        new Profile()
                        {
                            LocalPublishFolder="LocalPublishFolder",
                            Name="Name 2",
                            RemotePublishFolder="RemotePublishFolder"
                        }
                    }
                };

                Profiles = Configuration.Profiles;
                SelectedProfile = Profiles.First();
                return;
            }

            windowLogger = new WindowLogger(SetStatus, SetStatus, SetStatus, ShowError, null);
            deploymentService = new DeploymentService(windowLogger);
            deploymentService.ProgressChanged += DeploymentProgressChanged;

            _canExecute = true;
            compressionService = new CompressionService();
            Configuration = Utils.ReadConfiguration();
            IsAutoMode = Configuration.IsAutoMode;
            Profiles = Configuration.Profiles;
            SelectedProfile = Profiles.First();

            InitializeWatcher();
            Task.Run((Action)CheckHealth);
        }

        private void CheckHealth()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.PublishWebsiteUrl);
                    var result = client.GetStringAsync("/api/publish/health").Result;
                    SetStatus(result);
                }
            }
            catch (Exception e)
            {
                SetStatus(FormattableString.Invariant($"Health check failed : {e.Message}"));
            }
        }

        private void ZipSend()
        {
            _canExecute = false;
            var folderPath = SelectedProfile.LocalPublishFolder;
            Status = string.Empty;

            Task.Run(() =>
            {
                SetProgress(0);
                deploymentService.CompressAndSend(folderPath, SelectedProfile.RemotePublishFolder);
                _canExecute = true;
            });
        }

        private void DeploymentProgressChanged(HttpProgressEventArgs e)
        {
            SetProgress(e.ProgressPercentage);
            Console.WriteLine($"Percentage : {e.ProgressPercentage}, uploaded : {e.BytesTransferred / 1048576} MB, total : {e.TotalBytes / 1048576} MB");
        }

        private void SetProgress(int value)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Progress = value;

                RaisePropertyChanged("Progress");
            });
        }

        public void SetStatus(string message)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Status += $"{message}{Environment.NewLine}";
                RaisePropertyChanged("Status");
            });
        }

        public void ShowError(string message) => MessageBox.Show(message);

        private static long GetDirectorySize(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            var infoList = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            return infoList.Select(x => x.Length).Sum();
        }

        private void InitializeWatcher()
        {
            if (IsAutoMode)
            {
                Task.Run(() =>
                {
                    windowLogger.Info("Initialiazing watcher");
                    publishWatcher = new PublishWatcher(windowLogger, DeploymentProgressChanged);
                    publishWatcher.Initialize();
                });
            }
            else
            {
                publishWatcher.Dispose();
                publishWatcher = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}