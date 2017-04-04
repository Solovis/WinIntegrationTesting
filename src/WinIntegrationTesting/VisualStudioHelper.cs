using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WinIntegrationTesting
{
    public static class VisualStudioHelper
    {
        public static void ShowAttachDebuggerDialog(int processId)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "vsjitdebugger.exe",
                Arguments = $"-p {processId}"
            };
            Process.Start(processStartInfo);
        }

        public static string FindSolutionFolderForAssembly(Assembly assembly)
        {
            string solutionFile = FindSolutionFileForAssembly(assembly);
            string folder = Path.GetDirectoryName(solutionFile);

            return folder;
        }

        /// <summary>
        /// Attempt to find the solution file given the assembly that is part of the solution.
        /// This will crawl the hierarchy looking for a folder with .sln extension.
        /// </summary>        
        public static string FindSolutionFileForAssembly(Assembly assembly)
        {
            string location = assembly.Location;
            string folder = Path.GetDirectoryName(location);

            string solutionFile = FindSolutionFileForSubFolder(folder);
            return solutionFile;
        }


        public static string FindSolutionFolderForSubFolder(string subFolder)
        {
            string solutionFile = FindSolutionFileForSubFolder(subFolder);
            string folder = Path.GetDirectoryName(solutionFile);

            return folder;
        }

        /// <summary>
        /// Attempt to find the solution file given the assembly that is part of the solution.
        /// This will crawl the hierarchy looking for a folder with .sln extension.
        /// </summary>
        public static string FindSolutionFileForSubFolder(string subFolder)
        {
            string currentPath = subFolder;

            try
            {
                int maxTries = 15;
                while (maxTries > 0)
                {
                    string solutionFile = Directory.GetFiles(currentPath, "*.sln").FirstOrDefault();
                    if (solutionFile != null)
                    {
                        return solutionFile;
                    }

                    currentPath = Path.GetDirectoryName(currentPath);
                    maxTries--;
                }

                throw new Exception("Max depth search exceeded.");
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to find solution file: " + subFolder, ex);
            }
        }
    }
}
