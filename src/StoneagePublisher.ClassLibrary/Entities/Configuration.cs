using System.Collections.Generic;

namespace StoneagePublisher.ClassLibrary.Entities
{
    public class Configuration
    {
        public string PublishWebsiteUrl { get; set; }
        public IList<Profile> Profiles { get; set; }
    }
}