using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIntegrationTesting
{
    /// <summary>
    /// AzureComputeEmulator requires admin priveleges to control the compute emulator.
    /// </summary>
    public static class AzureComputeEmulator
    {     
        private static string EmulatorCsRunPath
        {
            get
            {
                string csrunPath = @"C:\Program Files\Microsoft SDKs\Azure\Emulator\csrun.exe";

                return csrunPath;
            }
        }

        private static string GetEmulatorCsRunPathAndCheckExists()
        {
            string csrunPath = EmulatorCsRunPath;
            if (!File.Exists(csrunPath))
            {
                throw new Exception("csrun.exe not found: " + csrunPath);
            }

            return csrunPath;
        }


        public static bool IsEmulatorRunning
        {
            get
            {
                var psi = new ProcessStartInfo();
                psi.FileName = GetEmulatorCsRunPathAndCheckExists();
                psi.Arguments = "/status";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardInput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                StringBuilder sb = new StringBuilder();

                DataReceivedEventHandler errorHandler = (object sender, DataReceivedEventArgs e) =>
                {
                    string line = e.Data;
                    if (line != null)
                    {
                        System.Diagnostics.Debug.WriteLine(line);
                        sb.Append(line);
                        sb.Append("\r\n");
                    }
                };

                DataReceivedEventHandler dataHandler = (object sender, DataReceivedEventArgs e) =>
                {
                    string line = e.Data;
                    if (line != null)
                    {
                        System.Diagnostics.Debug.WriteLine(line);
                        sb.Append(line);
                        sb.Append("\r\n");
                    }
                };

                Process p = new Process();
                p.StartInfo = psi;
                p.ErrorDataReceived += errorHandler;
                p.OutputDataReceived += dataHandler;
                p.EnableRaisingEvents = true;

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();

                string output = sb.ToString();
                if (output.IndexOf("is not running") != -1)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static void StopComputeEmulator()
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetEmulatorCsRunPathAndCheckExists(),
                Arguments = "/devfabric:shutdown",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var p = Process.Start(psi);
            p.WaitForExit();
        }

        /// <summary>
        /// Start Azure compute emulator.
        /// </summary>
        /// <param name="csxPath">Path to folder such as c:\code\MyProject\csx\Debug</param>
        /// <param name="configPath">Path to configuration file such as c:\code\MyProject\ServiceConfiguration.Local.cscfg</param>
        public static void StartEmulator(string csxPath, string configPath)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = GetEmulatorCsRunPathAndCheckExists();
            psi.Arguments = String.Format("\"{0}\" \"{1}\" /useiisexpress", csxPath, configPath);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            StringBuilder sb = new StringBuilder();
            StringBuilder errorSb = new StringBuilder();

            DataReceivedEventHandler errorHandler = (object sender, DataReceivedEventArgs e) =>
            {
                string line = e.Data;
                if (line != null)
                {
                    errorSb.AppendLine(line);
                    System.Diagnostics.Debug.WriteLine(line);
                }
            };

            DataReceivedEventHandler dataHandler = (object sender, DataReceivedEventArgs e) =>
            {
                string line = e.Data;
                if (line != null)
                {
                    sb.AppendLine(line);
                    System.Diagnostics.Debug.WriteLine(line);
                }
            };

            Process p = new Process();
            p.StartInfo = psi;
            p.ErrorDataReceived += errorHandler;
            p.OutputDataReceived += dataHandler;
            p.EnableRaisingEvents = true;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            string errorOutput = errorSb.ToString();
            if (errorOutput.Contains("does not exist") || errorOutput.Contains("error"))
            {
                throw new Exception(errorOutput);
            }
        }
    }
}
