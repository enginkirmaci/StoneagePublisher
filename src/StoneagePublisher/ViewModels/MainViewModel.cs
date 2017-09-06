using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using StoneagePublisher.ClassLibrary;
using StoneagePublisher.ClassLibrary.Command;
using StoneagePublisher.ClassLibrary.Entities;
using StoneagePublisher.ClassLibrary.Services;

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

        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Status = "Status";
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
                SetStatus("Status");
                SetStatus($"---------------- {DateTime.Now}");
                var rawSize = GetInMb(GetDirectorySize(folderPath));
                SetStatus("Initial folder size: " + rawSize.ToString("F") + " mb. ");

                SetStatus("Zip started.");
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var bytes = compressionService.GetZippedBytes(folderPath);
                var size = GetInMb(bytes.Length);
                SetStatus($"Zipping Done in {stopwatch.Elapsed}, Size: {size:F} mb. ");

                SetStatus("Upload started");
                var uploadStopwatch = new Stopwatch();
                uploadStopwatch.Start();
                HttpPost(SelectedProfile.RemotePublishFolder, bytes);

                SetStatus($"Upload done in {uploadStopwatch.Elapsed}");
                
                SetStatus($"Total Duration: {stopwatch.Elapsed}");
                uploadStopwatch.Stop();
                stopwatch.Stop();
                _canExecute = true;
            });
        }

        private void HttpPost(string webrootPath, byte[] bytes)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(Configuration.PublishWebsiteUrl);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/bson"));

                    var load = new
                    {
                        WebRootPath = webrootPath,
                        Bytes = bytes
                    };

                    MediaTypeFormatter bsonFormatter = new BsonMediaTypeFormatter();

                    var result = client.PostAsync(Configuration.PublishWebsitePath, load, bsonFormatter).Result;

                    if (result != null && result.IsSuccessStatusCode)
                    {
                        SetStatus(Environment.NewLine + "Upload and Publish done.");
                    }
                    else
                    {
                        var resultContent = result?.Content.ReadAsStringAsync().Result;

                        SetStatus(Environment.NewLine + "Upload failed.");
                        SetStatus(Environment.NewLine + resultContent);
                    }
                }

                SetStatus(Environment.NewLine + "Upload done.");
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SetStatus(string message)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Status += $"{message}{Environment.NewLine}";
                RaisePropertyChanged("Status");
            });
        }


        static long GetDirectorySize(string path)
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