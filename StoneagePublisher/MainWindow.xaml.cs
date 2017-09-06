using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using StoneagePublisher.ClassLibrary;
using StoneagePublisher.ClassLibrary.Entities;

namespace StoneagePublisher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration Configuration { get; set; }
        public Profile SelectedProfile { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Configuration = Utils.ReadConfiguration();
            Profiles.ItemsSource = Configuration.Profiles;
            SelectProfile(null);
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
                Status.Content += appendDate ? $"{DateTime.Now.ToString()} : {message}" : message;
            }));
        }

        private void ZipSend_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = LocalFolderPath.Text;
            var ftpPath = FtpPath.Text;

            var outputFolder = Environment.CurrentDirectory;
            var outputFileName = SelectedProfile.Name + ".zip";
            var outputFilePath = Path.Combine(outputFolder, outputFileName);

            var request = new PublishRequest()
            {
                FileName = outputFileName,
                WebRootPath = SelectedProfile.RemotePublishFolder
            };

            Status.Content = string.Empty;

            Task.Run(() =>
            {
                var startDate = DateTime.Now;
                SetStatus("Status" + Environment.NewLine + "----------------", false);
                SetStatus(Environment.NewLine + "Zip started.");
                CreatePublishZip(folderPath, outputFolder, outputFilePath);
                SetStatus(Environment.NewLine + "Upload started");
                HttpPost(ftpPath, outputFilePath, request);
                SetStatus(Environment.NewLine + $"Duration: {DateTime.Now.Subtract(startDate).ToString()}", false);
            });
        }

        private void HttpPost(string ftpPath, string outputFilePath, PublishRequest request)
        {
            try
            {
                var fileInfo = new FileInfo(outputFilePath);
                request.Bytes = new byte[fileInfo.Length];

                using (var fs = fileInfo.OpenRead())
                {
                    fs.Read(request.Bytes, 0, Convert.ToInt32(fileInfo.Length));
                }

                WebRequest wbRequest = WebRequest.Create(Configuration.PublishWebsiteUrl + $"?webRootPath={request.WebRootPath}&filename={request.FileName}");
                wbRequest.Method = "POST";
                wbRequest.ContentType = "application/x-www-form-urlencoded";
                wbRequest.ContentLength = request.Bytes.Length;

                using (Stream stream = wbRequest.GetRequestStream())
                {
                    stream.Write(request.Bytes, 0, request.Bytes.Length);
                }

                string responseContent = null;

                using (WebResponse response = wbRequest.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader streamReader = new StreamReader(stream))
                        {
                            responseContent = streamReader.ReadToEnd();
                        }
                    }
                }

                SetStatus(Environment.NewLine + "Upload done.");
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        public void ExtractZipFile(string archiveFilenameIn, string outFolder)
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(archiveFilenameIn);
                zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }

                SetStatus(Environment.NewLine + "UnZip done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }

        public void CreatePublishZip(string folderName, string outputFolder, string outputFilePath)
        {
            try
            {
                FileStream fsOut = File.Create(outputFilePath);
                ZipOutputStream zipStream = new ZipOutputStream(fsOut);

                zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

                // This setting will strip the leading part of the folder path in the entries, to
                // make the entries relative to the starting folder.
                // To include the full path for each entry up to the drive root, assign folderOffset = 0.
                int folderOffset = folderName.Length + (folderName.EndsWith("\\") ? 0 : 1);

                CompressFolder(folderName, zipStream, folderOffset);

                zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
                zipStream.Close();
                SetStatus(Environment.NewLine + "Zip done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {
                FileInfo fi = new FileInfo(filename);

                string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

                // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
                // A password on the ZipOutputStream is required if using AES.
                //   newEntry.AESKeySize = 256;

                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                // but the zip will be in Zip64 format which not all utilities can understand.
                //   zipStream.UseZip64 = UseZip64.Off;
                newEntry.Size = fi.Length;

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
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