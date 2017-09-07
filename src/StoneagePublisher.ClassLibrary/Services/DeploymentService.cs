using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Handlers;
using System.Net.Http.Headers;
using System.Threading;
using StoneagePublisher.ClassLibrary.Entities;

namespace StoneagePublisher.ClassLibrary.Services
{
    public class DeploymentService
    {
        private readonly ILogService logService;
        private readonly ConfigurationProvider configurationProvider;
        private readonly CompressionService compressionService;

        public event Action<HttpProgressEventArgs> ProgressChanged;

        public DeploymentService(ILogService logService)
        {
            this.logService = logService;
            configurationProvider = new ConfigurationProvider();
            compressionService = new CompressionService();
        }

        public bool CompressAndSend(string localFolderPath, string remoteFolderPath)
        {
            logService.Info("Status");
            logService.Info($"---------------- {DateTime.Now}");
            var rawSize = GetInMb(GetDirectorySize(localFolderPath));
            logService.Info("Initial folder size: " + rawSize.ToString("F") + " mb. ");

            logService.Info("Zip started.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var bytes = compressionService.GetZippedBytes(localFolderPath);
            var size = GetInMb(bytes.Length);
            logService.Info($"Zipping Done in {stopwatch.Elapsed}, Size: {size:F} mb. ");

            logService.Info("Upload started");
            var uploadStopwatch = new Stopwatch();
            uploadStopwatch.Start();
            var deployed = DeployData(remoteFolderPath, bytes);

            logService.Info($"Upload done in {uploadStopwatch.Elapsed}");

            logService.Info($"Total Duration: {stopwatch.Elapsed}");
            uploadStopwatch.Stop();
            stopwatch.Stop();

            return deployed;
        }

        private bool DeployData(string webrootPath, byte[] bytes)
        {
            var configuration = configurationProvider.Getconfiguration();
            try
            {
                var httpProgressHandler = new ProgressMessageHandler(new HttpClientHandler());
                httpProgressHandler.HttpSendProgress += HttpProgressHandlerOnHttpSendProgress;

                using (var client = new HttpClient(httpProgressHandler))
                {
                    client.BaseAddress = new Uri(configuration.PublishWebsiteUrl);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));

                    var load = new
                    {
                        WebRootPath = webrootPath,
                        Bytes = bytes
                    };

                    var objectBuffer = new MemoryStream();
                    MediaTypeFormatter bsonFormatter = new BsonMediaTypeFormatter();
                    bsonFormatter.WriteToStreamAsync(load.GetType(), load, objectBuffer, null, null).Wait();
                    objectBuffer.Seek(0, SeekOrigin.Begin);
                    var uploadContent = new StreamContent(objectBuffer);
                    uploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/bson");

                    var result = client.PostAsync(configuration.PublishWebsitePath, uploadContent).Result;

                    if (result != null && result.IsSuccessStatusCode)
                    {
                        logService.Info(Environment.NewLine + "Upload and Publish done.");
                    }
                    else
                    {
                        var resultContent = result?.Content.ReadAsStringAsync().Result;

                        logService.Info("Upload failed.");
                        logService.Info(resultContent);
                    }
                }

                logService.Info(Environment.NewLine + "Upload done.");
                return true;
            }
            catch (WebException e)
            {
                logService.Error(e.Message);
            }
            catch (Exception ex)
            {
                logService.Error(ex.Message);
            }
            return false;
        }

        private float GetInMb(int byteLength) => (float)byteLength / (1024 * 1024);

        private float GetInMb(long byteLength) => (float)byteLength / (1024 * 1024);

        private static long GetDirectorySize(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            var infoList = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            return infoList.Select(x => x.Length).Sum();
        }

        private void HttpProgressHandlerOnHttpSendProgress(object sender, HttpProgressEventArgs e)
        {
            ProgressChanged?.Invoke(e);
        }
    }
}