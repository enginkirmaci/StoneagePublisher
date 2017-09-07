using System.Collections.Generic;

namespace StoneagePublisher.ClassLibrary.Entities
{
    public class Configuration
    {
        public bool IsAutoMode { get; set; }
        public string PublishWebsiteUrl { get; set; }
        public string PublishWebsitePath { get; set; }
        public IList<Profile> Profiles { get; set; }
    }
}