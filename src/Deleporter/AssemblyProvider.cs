using System;
using System.IO;
using System.Linq;

namespace DeleporterCore
{
    internal class AssemblyProvider : MarshalByRefObject
    {
        public byte[] GetAssembly(string assemblyName)
        {
            try
            {
                // If the assembly is already loaded, use that
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var matchingAssembly = assemblies.FirstOrDefault(x => x.FullName == assemblyName);
                // Otherwise, look on disk
                if (matchingAssembly == null)
                    matchingAssembly = AppDomain.CurrentDomain.Load(assemblyName);
                // Now return the assembly bytes
                if (matchingAssembly != null && matchingAssembly.Location != null) {
                    LoggerServer.Log("Assembly found by AssemblyProvider {0}", matchingAssembly.FullName);
                    return File.ReadAllBytes(matchingAssembly.Location);
                }
            }
            catch (FileNotFoundException) { /* Means the assembly wasn't known */ }

            return null;
        }

        public override object InitializeLifetimeService()
        {
            return null; // Don't expire this object
        }
    }
}