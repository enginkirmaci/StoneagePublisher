using System.Collections.ObjectModel;

namespace StoneagePublisher.ClassLibrary.Entities
{
    public class Configuration
    {
        public bool IsAutoMode { get; set; }
        public string PublishWebsiteUrl { get; set; }
        public string PublishWebsitePath { get; set; }
        public ObservableCollection<Profile> Profiles { get; set; }
    }
}