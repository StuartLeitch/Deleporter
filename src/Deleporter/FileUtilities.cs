using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DeleporterCore
{
    public static class FileUtilities
    {
        private static readonly HashSet<string> _searchedDirectories = new HashSet<string>();

        /// <summary>
        ///   Based on startDirectory, search up and down for directory that contains a file matching searchPattern.
        /// </summary>
        /// <param name="searchPattern"> DOS style search pattern (i.e. *.config) </param>
        /// <param name="startDirectory"> Starting folder for the search </param>
        /// <param name="searchDepth"> How many folders to look down at each level (if searchHeight > 0, we will search all children of the parent to this depth </param>
        /// <param name="searchHeight"> How far above the starting folder should we look </param>
        /// <returns> Full path of directory. Null if not found. </returns>
        public static string FindDirectoryContainingFile(string searchPattern, string startDirectory, int searchDepth, int searchHeight = 0) {
            LoggerServer.Log("Looking for {0} in {1}", searchPattern, startDirectory);

            string match = null;
            var dir = startDirectory;

            while (match == null && dir != null) {
                match = FindDownForDirectoryContainingFile(searchPattern, dir, searchDepth);

                var parent = searchHeight > 0 ? EatPermissionErrors(() => Directory.GetParent(dir).FullName) : null;
                if (searchHeight > 0) searchHeight--;

                dir = parent;
            }
            LoggerServer.Log("Found {0} in {1}", searchPattern, match);
            LoggerServer.Log("");
            return match;
        }

        /// <summary>
        ///   Based on startDirectory, search up and down for filePath of the first file matching searchPattern.
        /// </summary>
        /// <param name="searchPattern"> DOS style search pattern (i.e. *.config) </param>
        /// <param name="startDirectory"> Starting folder for the search </param>
        /// <param name="searchDepth"> How many folders to look down at each level (if searchHeight > 0, we will search all children of the parent to this depth </param>
        /// <param name="searchHeight"> How far above the starting folder should we look </param>
        /// <returns> Full path of first file file. Null if not found. </returns>
        public static string FindPathForFile(string searchPattern, string startDirectory, int searchDepth, int searchHeight) {
            var directoryMatch = FindDirectoryContainingFile(searchPattern, startDirectory, searchDepth, searchHeight);

            if (!string.IsNullOrWhiteSpace(directoryMatch)) return Directory.EnumerateFiles(directoryMatch, searchPattern).First();

            return null;
        }

        public static bool LocalPortIsAvailable(int port) {
            var localhost = Dns.GetHostAddresses("localhost")[0];

            try {
                var sock = new Socket(localhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(localhost, port);
                if (sock.Connected) // RemotingPort is in use and connection is successful
                {
                    sock.Disconnect(false);
                    sock.Dispose();
                    return false;
                    
                }

                throw new Exception("Not connected to port ... but no Exception was thrown?");
            } catch (SocketException ex) {
                if (ex.ErrorCode == 10061) // RemotingPort is unused and could not establish connection 
                    return true;
                throw ex;
            }
        }


        /// <summary>
        ///   Searches programFilesDirectoryToSearch in Program Files/Program Files (x86) for the filePath of the first file matching searchPattern. Will search down 5 directories.
        /// </summary>
        /// <param name="searchPattern"> DOS style search pattern (i.e. *.config) </param>
        /// <param name="programFilesDirectoryToSearch"> Starting folder for the search </param>
        /// <param name="searchAllProgramFiles"> Look in siblings of programFilesDirectoryToSearch? </param>
        /// <returns> Full path of first file file. Null if not found. </returns>
        public static string TryToFindProgramFile(string searchPattern, string programFilesDirectoryToSearch,
                                                  bool searchAllProgramFiles = false) {
            var directoriesToLookIn = new List<string>
            {
                    Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "", programFilesDirectoryToSearch),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), programFilesDirectoryToSearch),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), programFilesDirectoryToSearch),
            };

            foreach (var directoryToLookIn in directoriesToLookIn) {
                var foundFile = FindPathForFile(searchPattern, directoryToLookIn, 5, searchAllProgramFiles ? 1 : 0);
                if (foundFile != null) return foundFile;
            }

            return null;
        }


        [DebuggerStepThrough]
        private static T EatPermissionErrors<T>(Func<T> method) {
            try {
                return method.Invoke();
            } catch (UnauthorizedAccessException unauthorizedAccessException) {
                LoggerServer.Log("Hit a directory permission error {0}", unauthorizedAccessException.Message);
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        private static string FindDownForDirectoryContainingFile(string searchPattern, string rootDirectoryToLookIn, int searchDepth) {
            if (_searchedDirectories.Contains(rootDirectoryToLookIn + searchPattern)) return null;
            _searchedDirectories.Add(rootDirectoryToLookIn + searchPattern);

            if (!Directory.Exists(rootDirectoryToLookIn)) return null;

            LoggerServer.Log("Searching {0}", rootDirectoryToLookIn);

            if (EatPermissionErrors(() => Directory.EnumerateFiles(rootDirectoryToLookIn, searchPattern).Any())) return rootDirectoryToLookIn;

            if (searchDepth > 0) {
                searchDepth--;
                foreach (var dir in EatPermissionErrors(() => Directory.GetDirectories(rootDirectoryToLookIn))) {
                    var result = FindDownForDirectoryContainingFile(searchPattern, dir, searchDepth);
                    if (result != null) return result;
                }
            }

            return null;
        }
    }
}