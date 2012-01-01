using System.Configuration;
using System.IO;
using System.Linq;

namespace DeleporterCore.SelfHosting.SeleniumServer.Configuration
{
    public class DeleporterSeleniumServerConfiguration
    {
        internal const int DefaultSeleniumServerPort = 4444;

        static DeleporterSeleniumServerConfiguration() {
            // TODO Stuart: Test this works.
            var config = (DeleporterSeleniumServerConfigurationSection)ConfigurationManager.GetSection("deleporterSelfHostSelenium");
            if (config != null) {
                SeleniumServerPort = (config.SeleniumServerPort != 0) ? config.SeleniumServerPort : DefaultSeleniumServerPort;
                SeleniumServerJar = config.SeleniumServerJar ?? GetSeleniumServerJarLocation();
            } else {
                SeleniumServerPort = DefaultSeleniumServerPort;
                SeleniumServerJar = GetSeleniumServerJarLocation();
            }
        }

        public static string SeleniumServerJar { get; private set; }
        public static int SeleniumServerPort { get; private set; }

        private static string GetSeleniumServerJarLocation() {
            return FileUtilities.FindPathForFile("selenium-server-standalone*.jar", Directory.GetCurrentDirectory(), 2, 2);
        }
    }
}