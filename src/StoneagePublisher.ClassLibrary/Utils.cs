using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using StoneagePublisher.ClassLibrary.Entities;

namespace StoneagePublisher.ClassLibrary
{
    public class Utils
    {
        public static Configuration ReadConfiguration()
        {
            var configPath = Path.Combine(Environment.CurrentDirectory, "Config.json");
            using (var stream = GetFileStream(configPath))
            {
                return Deserialize<Configuration>(stream);
            }
        }

        private static T Deserialize<T>(Stream s)
        {
            using (var reader = new StreamReader(s, Encoding.UTF8))
            {
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }

        private static FileStream GetFileStream(string path)
        {
            var fi = new FileInfo(path);

            return fi.OpenRead();
        }
    }
}