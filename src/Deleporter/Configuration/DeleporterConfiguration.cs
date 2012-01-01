using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Configuration;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace DeleporterCore.Configuration
{
    public static class DeleporterConfiguration
    {
        internal const int DefaultRemotingPort = 38473;
        internal const string DefaultHost = "localhost";
        internal const string DefaultServiceName = "Deleporter.rm";
        internal const int DefaultWebHostPort = 0;
        internal const string DefaultRelativePathToWebApp = null;

        public static int RemotingPort { get; private set; }
        public static string Host { get; private set; }
        public static string ServiceName { get; private set; }
        public static int WebHostPort { get; private set; }
        public static string RelativePathToWebApp { get; private set; }
        public static string FullyQualifiedPathToWebApp { get; private set; }

        static DeleporterConfiguration()
        {
            DeleporterConfigurationSection config = null;
            // TODO Stuart: Figure out why this is happening.
            try { config = (DeleporterConfigurationSection)ConfigurationManager.GetSection("deleporter"); }
            catch (Exception ex)
            {
                Logger.Log("Error when checking where config was loaded from {0}", ex.Message);
            }
            if (config != null)
            {
                RemotingPort = config.RemotingPort;
                Host = config.Host;
                ServiceName = config.ServiceName;
                WebHostPort = config.WebHostPort;

            }
            else
            {
                RemotingPort = DefaultRemotingPort;
                Host = DefaultHost;
                ServiceName = DefaultServiceName;
                WebHostPort = DefaultWebHostPort;
            }

            // TODO Stuart: Figure out how to best call this.
            bool calledFromClient = false;
            try {
                calledFromClient = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath.EndsWith("dll.config");
            } catch (Exception ex) {
                
                Logger.Log("Error when checking where config was loaded from {0}", ex.Message);
            }

            if (calledFromClient)
            {
                if (!string.IsNullOrWhiteSpace(RelativePathToWebApp))
                {
                    FullyQualifiedPathToWebApp = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), RelativePathToWebApp));
                }
                else
                {
                    FullyQualifiedPathToWebApp = FileUtilities.FindDirectoryContainingFile("web.config", Directory.GetCurrentDirectory(), 2, 4);
                }

                // Get rest of settings out of web.config
                var webConfigPath = Path.Combine(FullyQualifiedPathToWebApp, "web.config");
                var configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = webConfigPath };
                var webConfig = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                try
                {
                    config = (DeleporterConfigurationSection)webConfig.GetSection("deleporter");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                if (config != null)
                {
                    RemotingPort = config.RemotingPort;
                    Host = config.Host;
                    ServiceName = config.ServiceName;
                    WebHostPort = config.WebHostPort;
                }
                else
                {
                    RemotingPort = DeleporterConfiguration.DefaultRemotingPort;
                    Host = DeleporterConfiguration.DefaultHost;
                    ServiceName = DeleporterConfiguration.DefaultServiceName;
                    WebHostPort = DeleporterConfiguration.DefaultWebHostPort;
                }

            }

        }

        public static IChannel CreateChannel()
        {
            IDictionary props = new Hashtable { { "port", RemotingPort }, { "typeFilterLevel", TypeFilterLevel.Full } };

            return new TcpChannel(props, null, new BinaryServerFormatterSinkProvider
            {
                TypeFilterLevel = TypeFilterLevel.Full
            });
        }

        public static string HostAddress
        {
            get { return String.Format("tcp://{0}:{1}/{2}", Host, RemotingPort, ServiceName); }
        }

        public static string SiteBaseUrl { get { return String.Format("http://{0}:{1}/", Host, WebHostPort); } }
    }
}