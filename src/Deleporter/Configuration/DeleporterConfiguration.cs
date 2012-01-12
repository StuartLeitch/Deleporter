using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace DeleporterCore.Configuration
{
    public static class DeleporterConfiguration
    {
        internal const string DefaultHost = "localhost";
        internal const bool DefaultLoggingEnabled = false;
        internal const string DefaultRelativePathToWebApp = null;
        internal const int DefaultRemotingPort = 38473;
        internal const int DefaultSeleniumServerPort = 4444;
        internal const string DefaultServiceName = "Deleporter.rm";
        internal const int DefaultWebHostPort = 0;
        public const bool DefaultDisabled = false;
        private static string _fullyQualifiedPathToWebApp;

        private static string _seleniumServerJar;
        private static System.Configuration.Configuration _webConfig;

        static DeleporterConfiguration() {
            Initialize();
        }

        public static string FullyQualifiedPathToWebApp {
            get {
                if (!string.IsNullOrWhiteSpace(_fullyQualifiedPathToWebApp))
                    return _fullyQualifiedPathToWebApp;
                if (!string.IsNullOrWhiteSpace(RelativePathToWebApp))
                    return
                            _fullyQualifiedPathToWebApp =
                            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativePathToWebApp));

                return
                        _fullyQualifiedPathToWebApp =
                        FileUtilities.FindDirectoryContainingFile("web.config", Directory.GetCurrentDirectory(), 2, 4);
            }
        }

        public static string Host { get; private set; }
        public static string HostAddress { get { return String.Format("tcp://{0}:{1}/{2}", Host, RemotingPort, ServiceName); } }
        public static bool LoggingEnabled { get; private set; }
        public static bool Disabled { get; private set; }
        public static string RelativePathToWebApp { get; private set; }
        public static int RemotingPort { get; private set; }
        public static string SeleniumServerJar { get {
            return !string.IsNullOrWhiteSpace(_seleniumServerJar) ? _seleniumServerJar : (_seleniumServerJar = GetSeleniumServerJarLocation());
        } }
        public static int SeleniumServerPort { get; private set; }
        public static string ServiceName { get; private set; }
        public static string SiteBaseUrl { get { return String.Format("http://{0}:{1}/", Host, WebHostPort); } }
        public static int WebHostPort { get; private set; }

        public static IChannel CreateChannel() {

            // TODO Stuart: Getting intermittent SocketExceptions here -
            // Only one usage of each socket address (protocol/network address/port) is normally permitted
            var registeredChannels = ChannelServices.RegisteredChannels;

            IDictionary props = new Hashtable { { "port", RemotingPort }, { "typeFilterLevel", TypeFilterLevel.Full } };

            return new TcpChannel(props, null, new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full });
        }

        public static void UpdatePortsInWebConfig(int newWebHostPort, int newRemotingPort) {
            var configSection = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");

            LogPortChanges(newWebHostPort, newRemotingPort, configSection);
            configSection.WebHostPort = WebHostPort = newWebHostPort;
            configSection.RemotingPort = RemotingPort = newRemotingPort;
            _webConfig.Save();
        }

        private static bool CallingAssemblyIsClient() {
            // TODO: Do this in a less clunky way.
            var calledFromClient = false;
            try {
                calledFromClient = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath.EndsWith("dll.config");
            } catch (Exception) {
                // Eat this exception.  Occurs when called from server.
            }
            return calledFromClient;
        }

        private static string GetSeleniumServerJarLocation() {
            return FileUtilities.FindPathForFile("selenium-server-standalone*.jar", Directory.GetCurrentDirectory(), 2, 2);
        }

        private static void Initialize() {
            var calledFromClient = CallingAssemblyIsClient();

            var config = (DeleporterConfigurationSection)ConfigurationManager.GetSection("deleporter");

            if (config != null) {
                RemotingPort = config.RemotingPort;
                Host = config.Host;
                Disabled = config.Disabled;
                ServiceName = config.ServiceName;
                WebHostPort = config.WebHostPort;
                LoggingEnabled = config.LoggingEnabled;
                RelativePathToWebApp = config.RelativePathToWebApp;
                SeleniumServerPort = (config.SeleniumServerPort != 0) ? config.SeleniumServerPort : DefaultSeleniumServerPort;
                _seleniumServerJar = config.SeleniumServerJar;
            } else {
                RemotingPort = DefaultRemotingPort;
                Host = DefaultHost;
                Disabled = DefaultDisabled;
                ServiceName = DefaultServiceName;
                WebHostPort = DefaultWebHostPort;
                LoggingEnabled = DefaultLoggingEnabled;
                RelativePathToWebApp = null;
                SeleniumServerPort = DefaultSeleniumServerPort;
                _seleniumServerJar = null;
            }

            if (calledFromClient)
                LoadConfigDataFromWebConfig(config);
        }

        private static void LoadConfigDataFromWebConfig(DeleporterConfigurationSection config) {
            // Get rest of settings out of web.config
            var webConfigPath = Path.Combine(FullyQualifiedPathToWebApp, "web.config");
            var configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = webConfigPath };
            _webConfig = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            try {
                config = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");
            } catch (Exception exception) {
                Console.WriteLine(exception);
            }
            if (config != null) {
                RemotingPort = config.RemotingPort;
                Host = config.Host;
                ServiceName = config.ServiceName;
                WebHostPort = config.WebHostPort;
                if (config.LoggingEnabled)
                    LoggingEnabled = config.LoggingEnabled;
            } else {
                RemotingPort = DefaultRemotingPort;
                Host = DefaultHost;
                ServiceName = DefaultServiceName;
                WebHostPort = DefaultWebHostPort;
            }
        }

        private static void LogPortChanges(int newWebHostPort, int newRemotingPort, DeleporterConfigurationSection configSection) {
            if (configSection.WebHostPort != newWebHostPort)
                LoggerClient.Log("Changing WebHostPort in web.config from {0} to {1}", configSection.WebHostPort, newWebHostPort);
            if (configSection.RemotingPort != newRemotingPort)
                LoggerClient.Log("Changing RemotingPort in web.config from {0} to {1}", configSection.WebHostPort, newWebHostPort);
        }
    }
}