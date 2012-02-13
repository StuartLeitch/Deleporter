using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Xml.Linq;

namespace DeleporterCore.Configuration
{
    public static class DeleporterConfiguration
    {
        public const bool DefaultBypassSelfHosting = false;
        public const bool DefaultDisabled = false;
        internal const bool DefaultLoggingEnabled = true;
        internal const string DefaultRelativePathToWebApp = null;
        internal const string DefaultRemotingHost = "localhost";
        internal const int DefaultRemotingPort = 38473;
        internal const int DefaultSeleniumServerPort = 4444;
        internal const string DefaultServiceName = "Deleporter.rm";
        internal const int DefaultWebHostPort = 51874;
        private static int? _csProjWebHostPort;
        private static string _fullyQualifiedPathToWebApp;
        private static string _seleniumServerJar;
        private static string _startingDirectoryForProjectContentSearches;
        private static System.Configuration.Configuration _webConfig;
        private static int _webHostPort;

        static DeleporterConfiguration() {
            Initialize();
        }

        public static bool BypassSelfHosting { get; private set; }

        public static int CsProjWebHostPort {
            get {
                if (_csProjWebHostPort.HasValue) return _csProjWebHostPort.Value;

                if (!File.Exists(WebApplicationCsProjPath)) throw new FileNotFoundException(string.Format("The .csproj file {0} was not found", WebApplicationCsProjPath));

                var inputStream = new FileStream(WebApplicationCsProjPath, FileMode.Open, FileAccess.Read);
                var streamReader = new StreamReader(inputStream);
                var result = streamReader.ReadToEnd();

                var csProjElement = XElement.Parse(result);

                // Currently only allowing for Cassini or IIS Express.  Consider supporting full IIS.

                var useIisElement = csProjElement.Descendants().First(x => x.Name.LocalName == "UseIIS");
                if (useIisElement == null) throw new InvalidOperationException("Was not able to find the IISUrl node in the web application .csproj file");
                var useIIS = bool.Parse(useIisElement.Value);

                if (useIIS) {
                    var iisUrlElement = csProjElement.Descendants().First(x => x.Name.LocalName == "IISUrl");
                    if (iisUrlElement == null) throw new InvalidOperationException("Was not able to find the IISUrl node in the web application .csproj file");
                    var iisUrl = iisUrlElement.Value;

                    var iisUri = new Uri(iisUrl);
                    _csProjWebHostPort = iisUri.Port;

                    return _csProjWebHostPort.Value;
                } else {
                    var developmentServerPortElement = csProjElement.Descendants().First(x => x.Name.LocalName == "DevelopmentServerPort");
                    if (developmentServerPortElement == null) throw new InvalidOperationException("Was not able to find the IISUrl node in the web application .csproj file");
                    _csProjWebHostPort = int.Parse(developmentServerPortElement.Value);

                    return _csProjWebHostPort.Value;
                }
            }
        }

        public static bool Disabled { get; private set; }

        public static string FullyQualifiedPathToWebApp {
            get {
                if (!string.IsNullOrWhiteSpace(_fullyQualifiedPathToWebApp)) return _fullyQualifiedPathToWebApp;
                if (!string.IsNullOrWhiteSpace(RelativePathToWebApp))
                    return
                            _fullyQualifiedPathToWebApp =
                            Path.GetFullPath(Path.Combine(StartingDirectoryForProjectContentSearches, RelativePathToWebApp));

                return
                        _fullyQualifiedPathToWebApp =
                        FileUtilities.FindDirectoryContainingFile("web.config", StartingDirectoryForProjectContentSearches, 2, 4);
            }
        }

        public static string Host { get; private set; }
        public static string HostAddress { get { return String.Format("tcp://{0}:{1}/{2}", Host, RemotingPort, ServiceName); } }
        public static bool LoggingEnabled { get; private set; }
        public static string RelativePathToWebApp { get; private set; }
        public static int RemotingPort { get; private set; }

        public static string SeleniumServerJar { get {
            return !string.IsNullOrWhiteSpace(_seleniumServerJar)
                           ? _seleniumServerJar : (_seleniumServerJar = GetSeleniumServerJarLocation());
        } }

        public static int SeleniumServerPort { get; private set; }
        public static string ServiceName { get; private set; }
        public static string SiteBaseUrl { get { return String.Format("http://{0}:{1}/", Host, WebHostPort); } }
        public static int WebHostPort { get { return BypassSelfHosting ? CsProjWebHostPort : _webHostPort; } private set { _webHostPort = value; } }

        private static string StartingDirectoryForProjectContentSearches {
            get {
                if (!string.IsNullOrWhiteSpace(_startingDirectoryForProjectContentSearches)) return _startingDirectoryForProjectContentSearches;

                var currentDirectory = Directory.GetCurrentDirectory();
                if (!currentDirectory.Contains("Program Files")) return _startingDirectoryForProjectContentSearches = currentDirectory;

                return _startingDirectoryForProjectContentSearches = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
            }
        }

        private static string WebApplicationCsProjPath { get { return Path.Combine(FullyQualifiedPathToWebApp, WebApplicationName + ".csproj"); } }
        private static string WebApplicationName { get { return FullyQualifiedPathToWebApp.Substring(FullyQualifiedPathToWebApp.LastIndexOf("\\", StringComparison.Ordinal) + 1); } }
        private static string WebConfigPath { get { return Path.Combine(FullyQualifiedPathToWebApp, "web.config"); } }

        public static IChannel CreateChannel() {
            // Only one usage of each socket address (protocol/network address/port) is normally permitted
            IDictionary props = new Hashtable { { "port", RemotingPort }, { "typeFilterLevel", TypeFilterLevel.Full } };

            return new TcpChannel(props, null, new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full });
        }

        /// <summary>
        ///   Updates the last write time on the web.config (causes app domain to recycle)
        /// </summary>
        public static void RecycleWebServerAppDomain() {
            File.SetLastWriteTime(WebConfigPath, DateTime.Now);
        }

        /// <summary>
        ///   Sets the web.config BypassSelfHosting setting to the value passed in.
        /// </summary>
        /// <param name="value"> true to bypass Self Hosting </param>
        public static void SetBypassSelfHosting(bool value = true) {
            var configSection = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");
            if (value == configSection.BypassSelfHosting) return;
            configSection.BypassSelfHosting = BypassSelfHosting = value;
            _webConfig.Save();
        }

        public static void UpdateRemotingPortInWebConfig(int newRemotingPort) {
            var configSection = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");
            LogPortChanges(newRemotingPort, configSection.RemotingPort, "RemotingPort");
            configSection.RemotingPort = RemotingPort = newRemotingPort;
            _webConfig.Save();
        }

        public static void UpdateWebHostPortInWebConfig(int newWebHostPort) {
            var configSection = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");
            LogPortChanges(newWebHostPort, configSection.RemotingPort, "WebHostPort");

            configSection.WebHostPort = WebHostPort = newWebHostPort;
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
            return FileUtilities.FindPathForFile("selenium-server-standalone*.jar", StartingDirectoryForProjectContentSearches, 2, 2);
        }

        private static void Initialize() {
            var calledFromClient = CallingAssemblyIsClient();
            _startingDirectoryForProjectContentSearches = null;

            var config = (DeleporterConfigurationSection)ConfigurationManager.GetSection("deleporter");

            if (config != null) {
                RemotingPort = config.RemotingPort;
                Host = config.RemotingHost;
                Disabled = config.Disabled;
                ServiceName = config.ServiceName;
                WebHostPort = config.WebHostPort;
                LoggingEnabled = config.LoggingEnabled;
                RelativePathToWebApp = config.RelativePathToWebApp;
                SeleniumServerPort = (config.SeleniumServerPort != 0) ? config.SeleniumServerPort : DefaultSeleniumServerPort;
                _seleniumServerJar = config.SeleniumServerJar;
                BypassSelfHosting = config.BypassSelfHosting;
            } else {
                RemotingPort = DefaultRemotingPort;
                Host = DefaultRemotingHost;
                Disabled = DefaultDisabled;
                ServiceName = DefaultServiceName;
                WebHostPort = DefaultWebHostPort;
                LoggingEnabled = DefaultLoggingEnabled;
                RelativePathToWebApp = null;
                SeleniumServerPort = DefaultSeleniumServerPort;
                _seleniumServerJar = null;
                BypassSelfHosting = DefaultBypassSelfHosting;
            }

            if (calledFromClient) LoadConfigDataFromWebConfig(config);
        }

        private static void LoadConfigDataFromWebConfig(DeleporterConfigurationSection config) {
            // Get rest of settings out of web.config
            var configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = WebConfigPath };
            _webConfig = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            try {
                config = (DeleporterConfigurationSection)_webConfig.GetSection("deleporter");
            } catch (Exception exception) {
                Console.WriteLine(exception);
            }
            if (config != null) {
                RemotingPort = config.RemotingPort;
                Host = config.RemotingHost;
                ServiceName = config.ServiceName;
                WebHostPort = config.WebHostPort;
                _seleniumServerJar = config.SeleniumServerJar;
                BypassSelfHosting = config.BypassSelfHosting;
                if (config.LoggingEnabled) LoggingEnabled = config.LoggingEnabled;
            } else {
                RemotingPort = DefaultRemotingPort;
                Host = DefaultRemotingHost;
                ServiceName = DefaultServiceName;
                WebHostPort = DefaultWebHostPort;
                _seleniumServerJar = null;
                BypassSelfHosting = DefaultBypassSelfHosting;
            }
        }

        private static void LogPortChanges(int newPort, int oldPort, string portType) {
            if (newPort != oldPort) LoggerClient.Log("Changing {0} in web.config from {1} to {2}", portType, oldPort, newPort);
        }
    }
}