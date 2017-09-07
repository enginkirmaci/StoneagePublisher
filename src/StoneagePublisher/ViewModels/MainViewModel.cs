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

namespace StoneagePublisher.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly CompressionService compressionService;
        private Profile _selectedProfile;

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
        private ICommand _zipSendCommand;

        public ICommand ZipSendCommand
        {
            get
            {
                return _zipSendCommand ?? (_zipSendCommand = new CommandHandler(() => ZipSend(), _canExecute));
            }
        }

        private bool _canExecute;

        public int Progress { get; set; }

        private readonly DeploymentService deploymentService;

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


            var logger = new WindowLogger(SetStatus, SetStatus, SetStatus, ShowError, null);
            deploymentService = new DeploymentService(logger);
            deploymentService.ProgressChanged += DeploymentProgressChanged;

            _canExecute = true;
            compressionService = new CompressionService();
            Configuration = Utils.ReadConfiguration();
            Profiles = Configuration.Profiles;
            SelectedProfile = Profiles.First();

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

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        private float GetInMb(int byteLength) => (float)byteLength / (1024 * 1024);

        private float GetInMb(long byteLength) => (float)byteLength / (1024 * 1024);
    }
}