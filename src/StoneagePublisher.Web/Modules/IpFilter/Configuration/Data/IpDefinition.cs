using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration.Data
{
    [DataContract]
    public class IpDefinition
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Values")]
        public IEnumerable<string> Ips { get; set; }
        
        [DataMember(Name = "Networks")]
        public IEnumerable<SubnetDefinition> SubnetDefinitions { get; set; }
    }
}