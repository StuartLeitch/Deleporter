using System;
using System.Linq;
using DeleporterCore.Configuration;

namespace DeleporterCore.SelfHosting.Servers
{
    public class Cassini : IServer
    {
        private static Microsoft.VisualStudio.WebHost.Server _casinniServer;
        private static bool _started;

        private static IServer _instance;
        public static IServer Instance { get { return _instance ?? (_instance = new Cassini()); } }

        public bool Start() {
            if (_started) return false;
            _started = true;

            DeleporterUtilities.SetWebAndRemotingPortsBasedOnAvailability();

            if (!DeleporterUtilities.LocalPortIsAvailable(DeleporterConfiguration.WebHostPort)) {
                LoggerClient.Log("ERROR: Cassini port {0} is being used. Attempt to start Cassini has been aborted",
                                        DeleporterConfiguration.WebHostPort);
                throw new InvalidOperationException(string.Format("Cassini port {0} is being used by something else. Attempt to start Cassini has been aborted",
                                        DeleporterConfiguration.WebHostPort));
            }

            LoggerClient.Log("Using web.config location {0} with port {1}",
                                    DeleporterConfiguration.FullyQualifiedPathToWebApp, DeleporterConfiguration.WebHostPort);

            _casinniServer = new Microsoft.VisualStudio.WebHost.Server(DeleporterConfiguration.WebHostPort, "/",
                                                                       DeleporterConfiguration.FullyQualifiedPathToWebApp);

            LoggerClient.Log("Cassini starting on port {0} using path {1}... ", DeleporterConfiguration.WebHostPort, DeleporterConfiguration.FullyQualifiedPathToWebApp);
            try {
                _casinniServer.Start();
            } catch (Exception ex) {
                LoggerClient.Log("Couldn't start Cassini ... {0}", ex.Message);
                return false;
            }

            DeleporterUtilities.WaitForLocalPortToBecomeUnavailable(DeleporterConfiguration.WebHostPort);
            LoggerClient.Log("Cassini Started");
            DeleporterUtilities.PrimeServerHomepage();
            return true;
        }

        public void Stop() {
            if (_casinniServer == null) {
                LoggerClient.Log("Cassini was not started by this process ... ");
                return;
            }

            LoggerClient.Log("Cassini stopping ... ");
           
            _casinniServer.Stop();
            _casinniServer.InitializeLifetimeService();
            try {
                DeleporterUtilities.WaitForLocalPortToBecomeAvailable(DeleporterConfiguration.WebHostPort);
                LoggerClient.Log("Cassini Stopped");
            }
            catch (Exception exception)
            {
                LoggerClient.Log("FAILURE: Cassini did not appear to release port {0} within 10 seconds.  Please make sure that Selenium Server is shut down first."
                    , DeleporterConfiguration.WebHostPort);
                throw new Exception(string.Format("Cassini did not appear to release port {0} within 10 seconds.  Please make sure that Selenium Server is shut down first."
                    , DeleporterConfiguration.WebHostPort), exception);
            } finally {
                LoggerClient.Dispose();
            }
        }
    }
}