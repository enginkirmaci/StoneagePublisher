using System.Net;
using StoneagePublisher.Web.Core;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration
{
    public class NetworkDefinition
    {
        public NetworkDefinition(string ipAddress, string subnetMask)
        {
            IpAddress = IPAddress.Parse(ipAddress);
            SubnetMask = IPAddress.Parse(subnetMask);
            NetworkAddress = IpAddress.GetNetworkAddress(SubnetMask);
        }

        public IPAddress IpAddress { get; }
        public IPAddress SubnetMask { get; }
        public IPAddress NetworkAddress { get; set; }
    }
}