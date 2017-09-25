using System.Runtime.Serialization;

namespace StoneagePublisher.Web.Modules.IpFilter.Configuration.Data
{
    [DataContract]
    public class SubnetDefinition
    {
        [DataMember(Name = "Ip")]
        public string IpAddress { get; set; }

        [DataMember(Name = "SubnetMask")]
        public string SubnetMask { get; set; }
    }
}