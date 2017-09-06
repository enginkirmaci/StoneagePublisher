using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using StoneagePublisher.ClassLibrary;
using StoneagePublisher.ClassLibrary.Entities;
using StoneagePublisher.ClassLibrary.Services;
using System.Net.Http.Formatting;
using System.Threading;

namespace StoneagePublisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration Configuration { get; set; }
        public Profile SelectedProfile { get; set; }

        private readonly CompressionService compressionService;

        public MainWindow()
        {
            InitializeComponent();

            Configuration = Utils.ReadConfiguration();
            Profiles.ItemsSource = Configuration.Profiles;
            SelectProfile(null);
            compressionService = new CompressionService();

            Task.Run((Action) CheckHealth);
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

        private void SelectProfile(string name)
        {
            var profile = !string.IsNullOrWhiteSpace(name) ? Configuration.Profiles.FirstOrDefault(i => i.Name == name) : Configuration.Profiles.FirstOrDefault();

            SelectedProfile = profile;
            Profiles.SelectedValue = profile;
            LocalFolderPath.Text = profile.LocalPublishFolder;
            FtpPath.Text = profile.RemotePublishFolder;
        }

        public void SetStatus(string message, bool appendDate = true)
        {
            Status.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                Status.Content += appendDate ? $"{DateTime.Now} : {message}" : message;
            }));
        }

        private void ZipSend_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = LocalFolderPath.Text;
            Status.Content = string.Empty;

            Task.Run(() =>
            {
                var startDate = DateTime.Now;
                SetStatus("Status" + Environment.NewLine + "----------------", false);
                SetStatus(Environment.NewLine + "Zip started.");
                var bytes = compressionService.GetZippedBytes(folderPath);
                SetStatus(Environment.NewLine + "Upload started");
                HttpPost(SelectedProfile.RemotePublishFolder, bytes);
                SetStatus(Environment.NewLine + $"Duration: {DateTime.Now.Subtract(startDate)}", false);
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

                    var resultContent = result.Content.ReadAsStringAsync().Result;
                    //TODO
                    SetStatus(resultContent);
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
        
        private void Profiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var profile = e.AddedItems[0] as Profile;
                SelectProfile(profile.Name);
            }
        }
    }
}