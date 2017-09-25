using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Caching;
using StoneagePublisher.Web.Core;
using StoneagePublisher.Web.Modules.IpFilter.Configuration;
using HostIpListDictionary = System.Collections.Generic.Dictionary<string, StoneagePublisher.Web.Modules.IpFilter.Configuration.WhitelistConfiguration>;

namespace StoneagePublisher.Web.Modules.IpFilter
{
    public class IpFilterModule : IHttpModule
    {
        private const string CacheKey = "Vacature_AllowedIps";
        private const string FileLocationSettingKey = "Security.AllowedIpsConfigPath";
        private const string DisabledSettingKey = "Security.IpFilterDisabled";

        private static readonly object LockObject = new object();

        private IpWhitelistConfigurationProvider configurationProvider;

        void IHttpModule.Dispose()
        {
        }

        void IHttpModule.Init(HttpApplication context)
        {
            configurationProvider = new IpWhitelistConfigurationProvider();
            context.BeginRequest += HandleBeginRequest;
        }

        private void HandleBeginRequest(object sender, EventArgs evargs)
        {
            var disabled = ConfigurationManager.AppSettings[DisabledSettingKey];
            if (disabled?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return;
            }

            var app = sender as HttpApplication;

            var ipAddress = app?.Context.Request.ServerVariables["REMOTE_ADDR"];
            if (string.IsNullOrEmpty(ipAddress))
            {
                return;
            }

            var configuration = GetConfiguration(app.Context, app?.Context.Request.Url.Host);

            if (configuration == null)
            {
                RejectRequest(app);
                return;
            }

            if (configuration.AllowedIps.Contains(ipAddress))
            {
                return;
            }

            if (configuration.Proxies.Contains(ipAddress))
            {
                if (GetForwardedIpAddresses(app.Context.Request).Any(x => configuration.AllowedIps.Contains(x)))
                {
                    return;
                }
            }

            IPAddress parsedIp;
            if (IPAddress.TryParse(ipAddress, out parsedIp))
            {
                if (configuration.Networks.Any(x => x.NetworkAddress.Equals(parsedIp.GetNetworkAddress(x.SubnetMask))))
                {
                    return;
                }
            }

            RejectRequest(app);
        }

        private static IEnumerable<string> GetForwardedIpAddresses(HttpRequest request)
        {
            var forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            var ips = forwardedFor.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = ips.Length + 1; i >= 0; i--)
            {
                yield return ips[i];
            }
        }

        private WhitelistConfiguration GetConfiguration(HttpContext context, string hostname)
        {
            var dictionary = context.Cache[CacheKey] as HostIpListDictionary;
            if (dictionary == null)
            {
                try
                {
                    if (Monitor.TryEnter(LockObject, 1000))
                    {
                        dictionary = context.Cache[CacheKey] as HostIpListDictionary;
                        if (dictionary != null)
                        {
                            return GetconfigFromDictionary(dictionary, hostname);
                        }

                        var path = GetAllowedIpsFilePath(context);
                        if (string.IsNullOrEmpty(path))
                        {
                            return null;
                        }

                        dictionary = GetConfiguration(path);
                        context.Cache.Insert(CacheKey, dictionary, new CacheDependency(path));
                    }
                }
                finally
                {
                    Monitor.Exit(LockObject);
                }
            }

            return GetconfigFromDictionary(dictionary, hostname);
        }

        private static WhitelistConfiguration GetconfigFromDictionary(HostIpListDictionary dictionary, string hostname) 
            => dictionary.ContainsKey(hostname) ? dictionary[hostname] : 
            dictionary.ContainsKey(string.Empty) ? dictionary[string.Empty] : null;

        private static string GetAllowedIpsFilePath(HttpContext context)
        {
            var path = ConfigurationManager.AppSettings[FileLocationSettingKey];

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Path.IsPathRooted(path) ? path : context.Server.MapPath(path);
        }

        private Dictionary<string, WhitelistConfiguration> GetConfiguration(string path)
        {
            var ipList = configurationProvider.GetConfiguration(path);

            return ipList.ToDictionary(ipConfig => ipConfig.HostName);
        }

        private static void RejectRequest(HttpApplication app)
        {
            app.Context.Response.StatusCode = 403;
            app.Context.Response.SuppressContent = true;
            app.Context.Response.End();
        }
    }
}