using System.Collections.Generic;
using System.Linq;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration
{
    public class WhitelistConfiguration
    {
        public string HostName { get; }
        
        public IEnumerable<string> AllowedIps { get; }

        public IEnumerable<string> Proxies { get; }

        public IEnumerable<NetworkDefinition> Networks { get; }

        public WhitelistConfiguration(string hostname, IEnumerable<string> allowedIps, IEnumerable<string> proxies, IEnumerable<NetworkDefinition> networkDefinitions)
        {
            HostName = hostname;
            AllowedIps = allowedIps.ToList();
            Proxies = proxies.ToList();
            Networks = networkDefinitions.ToList();
        }
    }
}