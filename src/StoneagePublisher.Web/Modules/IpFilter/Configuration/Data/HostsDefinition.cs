using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration.Data
{
    [DataContract]
    public class HostsDefinition
    {
        [DataMember(Name = "Hosts")]
        public IEnumerable<string> Hosts { get; set; }

        [DataMember(Name = "Proxies")]
        public IEnumerable<string> Proxies { get; set; }

        [DataMember(Name = "Ips")]
        public IEnumerable<IpDefinition> Ips { get; set; }
    }
}