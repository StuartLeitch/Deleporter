using System.Linq;
using System.Configuration;

namespace DeleporterCore.Configuration
{
    public class DeleporterConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("RemotingPort", DefaultValue = DeleporterConfiguration.DefaultRemotingPort)]
        public int RemotingPort {
            get { return (int) this["RemotingPort"]; }
            set { this["RemotingPort"] = value; }
        }

        [ConfigurationProperty("Host", DefaultValue = DeleporterConfiguration.DefaultHost)]
        public string Host {
            get { return (string)this["Host"]; }
            set { this["Host"] = value; }
        }

        [ConfigurationProperty("ServiceName", DefaultValue = DeleporterConfiguration.DefaultServiceName)]
        public string ServiceName {
            get { return (string)this["ServiceName"]; }
            set { this["ServiceName"] = value; }
        }

        [ConfigurationProperty("WebHostPort", DefaultValue = DeleporterConfiguration.DefaultWebHostPort)]
        public int WebHostPort
        {
            get { return (int)this["WebHostPort"]; }
            set { this["WebHostPort"] = value; }
        }

        [ConfigurationProperty("RelativePathToWebApp", DefaultValue = DeleporterConfiguration.DefaultRelativePathToWebApp)]
        public string RelativePathToWebApp
        {
            get { return this["RelativePathToWebApp"].ToString(); }
            set { this["RelativePathToWebApp"] = value; }
        }

    }
}