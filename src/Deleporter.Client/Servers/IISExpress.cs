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

            if (!DeleporterUtilities.LocalPortIsAvailable(DeleporterConfiguration.WebHostPort))
            {
                Logger.Log("IIS Express port {0} is being used. Attempt to start IIS Express has been aborted",
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

            Logger.Log("IIS Express Starting ... ");
            try {
                _iisExpressProcess.Start();
            } catch (SocketException ex) {
                Logger.Log("Couldn't start IIS Express ...  {0}", ex.Message);
                return false;
            }

            DeleporterUtilities.WaitForLocalPortToBecomeUnavailable(DeleporterConfiguration.WebHostPort);
            DeleporterUtilities.PrimeServerHomepage();
            Logger.Log("IIS Express Started");
            return true;
        }

        public void Stop() {
            if (_iisExpressProcess == null) {
                Logger.Log("IIS Express was not started by this process ... ");
                return;
            }

            Logger.Log("IIS Express Stopping ... ");
            _iisExpressProcess.Kill();
            DeleporterUtilities.WaitForLocalPortToBecomeAvailable(DeleporterConfiguration.WebHostPort);
            Logger.Log("IIS Express Stopped");
        }
    }
}