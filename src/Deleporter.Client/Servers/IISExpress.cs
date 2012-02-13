using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using DeleporterCore.Configuration;

namespace DeleporterCore.SelfHosting.Servers
{
    public class IISExpress : IServer
    {
        private static Process _iisExpressProcess;
        private static bool _started;

        private static IServer _instance;
        public static IServer Instance { get { return _instance ?? (_instance = new IISExpress()); } }

        public bool Start() {
            if (_started) return false;
            _started = true;

            LoggerClient.LoggingEnabled = DeleporterConfiguration.LoggingEnabled;

            DeleporterUtilities.IterateWebAndRemotingPortsIfNeeded();

            if (!DeleporterUtilities.LocalPortIsAvailable(DeleporterConfiguration.WebHostPort))
            {
                LoggerClient.Log("ERROR: IIS Express port {0} is being used. Attempt to start IIS Express has been aborted",
                                        DeleporterConfiguration.WebHostPort);
                return false;
            }

            var fileName = FileUtilities.TryToFindProgramFile("iisexpress.exe", "IIS Express");
            if (fileName == null) throw new FileNotFoundException("IIS Express was not found on this machine.");

            _iisExpressProcess = new Process
            {
                    StartInfo =
                            {
                                    FileName = fileName,
                                    Arguments =
                                            "/path:\"" + DeleporterConfiguration.FullyQualifiedPathToWebApp + "\" /port:"
                                            + DeleporterConfiguration.WebHostPort + " /trace:error",
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                            }
            };

            LoggerClient.Log("IIS Express starting on port {0} using path {1}... ", DeleporterConfiguration.WebHostPort, DeleporterConfiguration.FullyQualifiedPathToWebApp);
            try {
                _iisExpressProcess.Start();
            } catch (SocketException ex) {
                LoggerClient.Log("Couldn't start IIS Express ...  {0}", ex.Message);
                return false;
            }

            DeleporterUtilities.WaitForLocalPortToBecomeUnavailable(DeleporterConfiguration.WebHostPort);
            DeleporterUtilities.PrimeServerHomepage();
            LoggerClient.Log("IIS Express Started");
            return true;
        }

        public void Stop() {
            if (_iisExpressProcess == null) {
                LoggerClient.Log("IIS Express was not started by this process ... ");
                return;
            }

            LoggerClient.Log("IIS Express Stopping ... ");
            _iisExpressProcess.Kill();

            try
            {
                DeleporterUtilities.WaitForLocalPortToBecomeAvailable(DeleporterConfiguration.WebHostPort);
                LoggerClient.Log("IIS Express Stopped");
            }
            catch (Exception exception)
            {
                var message = string.Format("IIS Express did not appear to release port {0} within 10 seconds.  " + 
                            "Please make sure that Selenium Server is shut down first.", DeleporterConfiguration.WebHostPort);
                LoggerClient.Log(message);
                throw new Exception(message, exception);
            }
            finally
            {
                LoggerClient.Dispose();
            }

        }
    }
}