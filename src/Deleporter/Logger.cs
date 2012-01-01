using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DeleporterCore
{
    internal static class Logger
    {
        private static StreamWriter _logWriter;

        private static StreamWriter LogWriter
        {
            get
            {
                try {
                    if (_logWriter != null) return _logWriter;
                    _logWriter = new StreamWriter(Path.Combine(Path.GetTempPath(), "DeleporterServer.log"), false);
                    _logWriter.WriteLine("Starting Server log session {0}", DateTime.Now);
                    _logWriter.Flush();
                    return _logWriter;
                } catch (Exception exception) {
                    Debug.WriteLine(exception);
                }

                return null;
            }
        }

        public static bool LoggingEnabled { get; set; }

        [DebuggerStepThrough]
        public static void Log(string message, params object[] args)
        {
            if (!LoggingEnabled || LogWriter == null) return;

            Debug.WriteLine(message, args);

            try {
                LogWriter.WriteLine(String.Format("{0:D2}:{1:D3}", DateTime.Now.Second, DateTime.Now.Millisecond) + " " + message, args);
                LogWriter.Flush();
            } catch (Exception exception) {
                // No point blowing up the app just because we can't log.
                Debug.WriteLine(exception.Message);
            }
        }

    }
}