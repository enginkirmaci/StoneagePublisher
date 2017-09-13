using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using StoneagePublisher.Web.Modules.IpFilter.Configuration.Data;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration
{
    public class IpWhitelistConfigurationProvider
    {
        public IEnumerable<WhitelistConfiguration> GetConfiguration(string path)
        {
            var definitions = ReadHostsDefinitions(path);

            var result = new List<WhitelistConfiguration>();

            foreach (var config in definitions)
            {
                var ips = config.Ips.Where(x => x.Ips != null).SelectMany(x => x.Ips).ToList();
                var networks = config.Ips.Where(x => x.SubnetDefinitions != null).SelectMany(x => x.SubnetDefinitions).Select(x => new NetworkDefinition(x.IpAddress, x.SubnetMask)).ToList();

                if (!config.Hosts.Any())
                {
                    result.Add(new WhitelistConfiguration(String.Empty, ips, config.Proxies, networks));
                }
                else
                {
                    foreach (var host in config.Hosts)
                    {
                        result.Add(new WhitelistConfiguration(host, ips, config.Proxies, networks));
                    }
                }
            }

            return result;
        }

        private static IEnumerable<HostsDefinition> ReadHostsDefinitions(string path)
        {
            if (!File.Exists(path))
            {
                throw new ConfigurationErrorsException(FormattableString.Invariant($"Could not read ip whitelist configuration from path {path}, file does not exist"));
            }

            var content = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<IEnumerable<HostsDefinition>>(content);
        }
    }
}