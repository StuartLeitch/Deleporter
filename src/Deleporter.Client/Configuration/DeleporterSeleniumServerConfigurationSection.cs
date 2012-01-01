using System.Linq;
using System.Configuration;

namespace DeleporterCore.SelfHosting.SeleniumServer.Configuration
{
    public class DeleporterSeleniumServerConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("SeleniumServerPort", DefaultValue = DeleporterSeleniumServerConfiguration.DefaultSeleniumServerPort)]
        public int SeleniumServerPort
        {
            get { return (int)this["SeleniumServerPort"]; }
            set { this["SeleniumServerPort"] = value; }
        }

        [ConfigurationProperty("SeleniumServerJar")]
        public string SeleniumServerJar
        {
            get { return this["SeleniumServerJar"].ToString(); }
            set { this["SeleniumServerJar"] = value; }
        }
    }
}