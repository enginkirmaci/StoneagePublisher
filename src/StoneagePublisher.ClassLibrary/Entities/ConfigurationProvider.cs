namespace StoneagePublisher.ClassLibrary.Entities
{
    public class ConfigurationProvider
    {
        public Configuration Getconfiguration() => Utils.ReadConfiguration();

        public event System.Action<Configuration> ConfigurationChanged;
    }
}
