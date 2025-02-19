using Microsoft.PowerPlatform.Dataverse.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DeployWeb.Models
{
    [DataContract]
    public class Connection
    {
        [DataMember(Name = "guid")]
        public Guid guid { get; set; }
        [DataMember(Name = "name")]
        public string name { get; set; }
        [DataMember(Name = "url")]
        public string url { get; set; }
        [DataMember(Name = "clientId")]
        public string clientId { get; set; }
        [DataMember(Name = "clientSecret")]
        public string clientSecret { get; set; }

        public override string ToString() { return this.name; }

        public ServiceClient GetService() {
            var connectionString = $"AuthType=ClientSecret;Url={url};ClientId={clientId};ClientSecret={clientSecret}";
            return new ServiceClient(connectionString);
        }
    }

    [DataContract]
    public class ConnectionsJson
    {
        [DataMember(Name = "connections")]
        public List<Connection> connections { get; set; }
    }
}
