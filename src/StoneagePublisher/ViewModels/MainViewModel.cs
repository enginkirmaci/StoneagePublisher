using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
        private readonly ConfigurationProvider configurationProvider;

        private PublishWatcher publishWatcher;
        private Profile _selectedProfile;
        private ICommand _zipSendCommand;
        private ICommand _autoModeCommand;
        private ICommand _saveCommand;
        private ICommand _newProfileCommand;

        public Profile SelectedProfile
        {
            get { return _selectedProfile == null ? Configuration.Profiles.First() : _selectedProfile; }
            set
            {
                _selectedProfile = value;
                RaisePropertyChanged("SelectedProfile");
            }
        }

        public Configuration Configuration { get; set; }
        public string Status { get; set; }
        public bool IsAutoMode { get; set; }
        public int Progress { get; set; }

        public ICommand ZipSendCommand => _zipSendCommand ?? (_zipSendCommand = new CommandHandler(() =>
        {
            var folderPath = SelectedProfile.LocalPublishFolder;
            Status = string.Empty;

            Task.Run(() =>
            {
                SetProgress(0);
                deploymentService.CompressAndSend(folderPath, SelectedProfile.RemotePublishFolder);
            });
        }, true));

        public ICommand AutoModeCommand => _autoModeCommand ?? (_autoModeCommand = new CommandHandler(() =>
        {
            IsAutoMode = IsAutoMode ? false : true;
            InitializeWatcher();
            Status = string.Empty;
            RaisePropertyChanged("Status");
            RaisePropertyChanged("IsAutoMode");
        }, true));

        public ICommand SaveCommand => _saveCommand ?? (_saveCommand = new CommandHandler(() =>
        {
            Configuration.IsAutoMode = IsAutoMode;
            configurationProvider.SetConfiguration(Configuration);
        }, true));

        public ICommand NewProfileCommand => _newProfileCommand ?? (_newProfileCommand = new CommandHandler(() =>
        {
            if (Configuration.Profiles.Any(i => i.Name == string.Empty))
                return;

            var newProfile = new Profile() { Name = string.Empty };
            Configuration.Profiles.Add(newProfile);
            SelectedProfile = newProfile;
        }, true));

        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                IsAutoMode = false;
                Status = "Status";
                Progress = 35;
                Configuration = new Configuration()
                {
                    Profiles = new ObservableCollection<Profile>()
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

                return;
            }

            windowLogger = new WindowLogger(SetStatus, SetStatus, SetStatus, ShowError, null, SetProgress);
            deploymentService = new DeploymentService(windowLogger);
            compressionService = new CompressionService();
            configurationProvider = new ConfigurationProvider();

            Configuration = configurationProvider.GetConfiguration();
            IsAutoMode = Configuration.IsAutoMode;

            InitializeWatcher();
            Task.Run((Action)CheckHealth);
        }

        private void InitializeWatcher()
        {
            if (IsAutoMode)
            {
                Task.Run(() =>
                {
                    windowLogger.Info("Initialiazing watcher");
                    publishWatcher = new PublishWatcher(windowLogger);
                    publishWatcher.Initialize();
                });
            }
            else if (publishWatcher != null)
            {
                publishWatcher.Dispose();
                publishWatcher = null;
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}