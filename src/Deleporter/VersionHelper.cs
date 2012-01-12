using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeleporterCore
{
    public static class VersionHelper
    {
        private static string _assemblyVersion;
        private static object _thisLock = new object();

        public static string AssemblyVersion
        {
            get
            {
                lock (_thisLock)
                {
                    if (!string.IsNullOrWhiteSpace(_assemblyVersion))
                    {
                        return _assemblyVersion;
                    }
                    return _assemblyVersion = GetAssemblyVersion();
                }
            }
        }

        private static string GetAssemblyVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return string.Format("{0} version {1}", fileVersionInfo.InternalName, fileVersionInfo.ProductVersion);
        }
    }
}
