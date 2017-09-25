using System;
using System.IO;
using Newtonsoft.Json;

namespace StoneagePublisher.ClassLibrary.Entities
{
    public class ConfigurationProvider
    {
        private readonly string ConfigPath;

        public ConfigurationProvider()
        {
            ConfigPath = Path.Combine(Environment.CurrentDirectory, "Config.json");
        }

        public Configuration GetConfiguration()
        {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigPath));
        }

        public void SetConfiguration(Configuration configuration)
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }
    }
}