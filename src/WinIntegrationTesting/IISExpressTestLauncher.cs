using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WinIntegrationTesting
{
    public class IISExpressTestLauncher
    {
        private const string TempWebFolderIdFilename = "TEMP_WEB_FOLDER.txt";

        private StartIISExpressOptions options;
        
        private string tempWebFolder;
        private Process iisExpressProcess;

        private IISExpressTestLauncher(StartIISExpressOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;

            this.SetupTempWebFolder();
            this.LaunchIISExpressProcess();
        }

        public static IISExpressTestLauncher StartIISExpress(
            StartIISExpressOptions options)
        {
            return new IISExpressTestLauncher(options);
        }


        public Process IISExpressProcess
        {
            get { return this.iisExpressProcess; }
        }

        public string TempWebFolder
        {
            get { return this.tempWebFolder; }
        }


        private void SetupTempWebFolder()
        {
            string tempFolderPath = options.TempFolderPath;
            if (tempFolderPath != null)
            {
                if (!Directory.Exists(tempFolderPath))
                {
                    string parentPath = Path.GetDirectoryName(tempFolderPath);
                    if (Directory.Exists(parentPath))
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }
                    else
                    {
                        throw new Exception($"Temporary folder does not exist: {tempFolderPath}");
                    }
                }

                var files = Directory.GetFiles(tempFolderPath);
                if (files != null && files.Length > 0)
                {
                    throw new Exception($"Temporary folder is not empty: {tempFolderPath}");
                }
            }
            else
            {
                tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempFolderPath);
            }

            if (tempFolderPath.Contains("\""))
            {
                throw new Exception($"tempFolder path can not contain quotation mark: {tempFolderPath}");
            }

            this.tempWebFolder = tempFolderPath;

            File.WriteAllText(
                Path.Combine(this.tempWebFolder, TempWebFolderIdFilename),
                "This is a temporary web folder for IIS Express test.");

            this.SetupWebConfig();
            this.SetupApplicationHostConfig();
            this.CreateLinksOrCopiesToOriginalProject();

            if (this.options.SetupTempFolderCallback != null)
            {
                this.options.SetupTempFolderCallback(tempWebFolder);
            }
        }

        private void SetupWebConfig()
        {
            if (this.options.WebProjectFolderPath == null)
            {
                throw new Exception("options.WebProjectFolderPath not specified.");
            }
            else
            {
                if (!Directory.Exists(options.WebProjectFolderPath))
                {                    
                    throw new Exception($"options.WebProjectFolderPath does not exist: {options.WebProjectFolderPath}");
                }
            }

            string originalWebConfigPath = Path.Combine(options.WebProjectFolderPath, "Web.config");
            if (!File.Exists(originalWebConfigPath))
            {
                throw new Exception($"Web.config not found: {originalWebConfigPath}");
            }

            string webConfigContentsToUse = null;
            if (this.options.TempWebConfigContents != null)
            {
                webConfigContentsToUse = this.options.TempWebConfigContents;
            }
            else
            {
                webConfigContentsToUse = File.ReadAllText(originalWebConfigPath);
            }

            // Replace settings in the config file as necessary.
            var xDoc = XDocument.Parse(webConfigContentsToUse);
            var configurationElement = xDoc.Element("configuration");
            if (configurationElement == null)
            {
                throw new Exception("configuration element not found");
            }

            if (this.options.AppSettings.Count > 0)
            {
                var appSettings = configurationElement.Element("appSettings");
                if (appSettings == null)
                {
                    throw new Exception("appSettings element not found");
                }
                
                HashSet<string> keysFoundSet = new HashSet<string>();

                // Overwrite any keys that already exist.
                foreach (var add in appSettings.Elements("add"))
                {
                    string name = add.Attribute("key")?.Value;
                    if (this.options.AppSettings.ContainsKey(name))
                    {
                        string newValue = this.options.AppSettings[name];
                        add.SetAttributeValue("value", newValue);

                        keysFoundSet.Add(name);
                    }
                }

                // Add values for any keys that didn't already exist.
                foreach (var pair in this.options.AppSettings)
                {
                    if (!keysFoundSet.Contains(pair.Key))
                    {
                        var newAdd = new XElement("add");
                        newAdd.SetAttributeValue("key", pair.Key);
                        newAdd.SetAttributeValue("value", pair.Value);

                        appSettings.Add(newAdd);
                    }
                }
            }            

            webConfigContentsToUse = xDoc.ToString();

            string tempWebConfigPath = Path.Combine(this.tempWebFolder, "Web.config");
            File.WriteAllText(tempWebConfigPath, webConfigContentsToUse);
        }


        private void SetupApplicationHostConfig()
        {
            string applicationHostConfigContentsToUse = null;

            if (this.options.TempApplicationHostConfigContents != null)
            {
                applicationHostConfigContentsToUse = this.options.TempApplicationHostConfigContents;
            }
            else
            {
                string solutionFolder = VisualStudioHelper.FindSolutionFolderForSubFolder(this.options.WebProjectFolderPath);
                string defaultApplicationHostConfigPath = Path.Combine(solutionFolder, ".vs", "config", "applicationhost.config");

                if (!File.Exists(defaultApplicationHostConfigPath))
                {
                    throw new Exception($"Unable to find applicationhost.config: {defaultApplicationHostConfigPath}");
                }

                applicationHostConfigContentsToUse = File.ReadAllText(defaultApplicationHostConfigPath);
            }

            string tempApplicationHostConfigPath = Path.Combine(this.tempWebFolder, "applicationhost.config");

            // Replace settings in the config file as necessary.
            var xDoc = XDocument.Parse(applicationHostConfigContentsToUse);
            var appHostElement = xDoc.Elements("configuration").First().Elements("system.applicationHost").First();

            var sitesElement = appHostElement.Elements("sites").First();

            string siteName = Path.GetFileName(this.options.WebProjectFolderPath);
            XElement site = null;
            List<XElement> sitesToRemove = new List<XElement>();
            foreach (var childSite in sitesElement.Elements("site"))
            {
                if (childSite.Attribute("name")?.Value == siteName)
                {
                    site = childSite;                    
                }
                else
                {
                    // We remove all but the desired site.
                    sitesToRemove.Add(childSite);
                }
            }

            if (site == null)
            {
                throw new Exception($"site element not found: name={siteName}");
            }

            foreach (var siteToRemove in sitesToRemove)
            {
                siteToRemove.Remove();
            }

            XElement application = site.Element("application");
            if (application == null)
            {
                throw new Exception($"application element not found: name={siteName}");
            }

            XElement virtualDirectory = application.Element("virtualDirectory");
            if (virtualDirectory == null)
            {
                throw new Exception($"virtualDirectory element not found: name={siteName}");
            }

            virtualDirectory.SetAttributeValue("physicalPath", this.tempWebFolder);

            // See if we need to replace the port.
            if (this.options.HttpPort != null || this.options.HttpsPort != null || this.options.HostName != null)
            {
                XElement bindings = site.Element("bindings");
                if (bindings != null)
                {
                    foreach (XElement binding in bindings.Elements("binding"))
                    {
                        string protocol = binding.Attribute("protocol")?.Value;
                        if (protocol == "http" || protocol == "https")
                        {
                            string bindingInformation = binding.Attribute("bindingInformation")?.Value;
                            if (bindingInformation != null)
                            {
                                string[] bindingInformationParts = bindingInformation.Split(':');                                
                                // TODO: Decide what to do if there are not already 3 parts.

                                if (bindingInformationParts.Length >= 2)
                                {
                                    if (protocol == "http" && this.options.HttpPort != null)
                                    {
                                        bindingInformationParts[1] = this.options.HttpPort.ToString();
                                    }
                                    else if (protocol == "https" && this.options.HttpsPort != null)
                                    {
                                        bindingInformationParts[1] = this.options.HttpsPort.ToString();
                                    }                                    
                                }

                                if (bindingInformationParts.Length >= 3)
                                {
                                    if (this.options.HostName != null)
                                    {
                                        bindingInformationParts[2] = this.options.HostName;
                                    }
                                }

                                string newBindingInformation = String.Join(":", bindingInformationParts);
                                binding.SetAttributeValue("bindingInformation", newBindingInformation);
                            }
                        }
                    }
                }
            }

            applicationHostConfigContentsToUse = xDoc.ToString();

            File.WriteAllText(tempApplicationHostConfigPath, applicationHostConfigContentsToUse);
        }


        private void CreateLinksOrCopiesToOriginalProject()
        {          
            // We copy top-level files since IIS Express wants a real file to monitor in some cases.
            foreach (var fullFilename in Directory.GetFiles(this.options.WebProjectFolderPath))
            {
                string filename = Path.GetFileName(fullFilename);
                if (!this.options.FilesToIgnore.Contains(filename))
                {
                    string filenameLower = filename.ToLower();
                    if (filenameLower != "web.config" && filenameLower != "applicationhost.config")
                    {
                        string targetFilename = Path.Combine(this.tempWebFolder, filename);
                        string sourceFilename = Path.Combine(this.options.WebProjectFolderPath, filename);

                        File.Copy(sourceFilename, targetFilename);
                    }
                }
            }

            // We create junctions to child folders to avoid having to copy too many files.
            List<Process> pendingJunctions = new List<Process>();

            foreach (var fullFilename in Directory.GetDirectories(this.options.WebProjectFolderPath))
            {
                string filename = Path.GetFileName(fullFilename);
                if (!this.options.FilesToIgnore.Contains(filename))
                {
                    string junctionFilename = Path.Combine(this.tempWebFolder, filename);
                    string sourceFilename = Path.Combine(this.options.WebProjectFolderPath, filename);

                    if (junctionFilename.Contains("\"") || sourceFilename.Contains("\""))
                    {
                        throw new Exception("filename contains quotation mark");
                    }

                    string escapedArgs = "\"" + junctionFilename + "\"" + " \"" + sourceFilename + "\"";

                    var mklinkStartInfo = new ProcessStartInfo("cmd.exe", "/c mklink /j " + escapedArgs)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    pendingJunctions.Add(Process.Start(mklinkStartInfo));
                }
            }

            foreach (var pendingJunction in pendingJunctions)
            {
                pendingJunction.WaitForExit();
            }
        }


        private void LaunchIISExpressProcess()
        {
            string applicationHostConfigPath = Path.Combine(this.tempWebFolder, "applicationhost.config");            

            var arguments = new StringBuilder();
            arguments.Append($"\"/config:{applicationHostConfigPath}\"");
            
            string iisexpressPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\IIS Express\\iisexpress.exe";
            if (!File.Exists(iisexpressPath))
            {
                throw new Exception($"IIS Express exe not found: {iisexpressPath}");
            }

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = iisexpressPath,
                Arguments = arguments.ToString(),                
                UseShellExecute = false,
                CreateNoWindow = !this.options.ShowIISExpressConsole
            };

            this.iisExpressProcess = Process.Start(processStartInfo);

            DeleteTempWebFolderAfterIISExpressExits(this.tempWebFolder, this.iisExpressProcess.Id);

            if (this.options.ShowAttachDebuggerDialog)
            {
                VisualStudioHelper.ShowAttachDebuggerDialog(this.iisExpressProcess.Id);
            }
        }


        public static void DeleteTempWebFolder(string tempWebFolder)
        {
            string idFilePath = Path.Combine(tempWebFolder, TempWebFolderIdFilename);
            if (!File.Exists(idFilePath))
            {
                throw new Exception("Temp web folder id file not found: " + idFilePath);
            }

            foreach (string file in Directory.GetFiles(tempWebFolder))
            {
                File.Delete(file);
            }

            // The directories will all be junctions so unlink them.
            List<Process> pendingJunctions = new List<Process>();

            foreach (string dir in Directory.GetDirectories(tempWebFolder))
            {
                string escapedArgs = "\"" + dir + "\"";

                var rmdirStartInfo = new ProcessStartInfo("cmd.exe", "/c rmdir " + escapedArgs)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                pendingJunctions.Add(Process.Start(rmdirStartInfo));
            }

            foreach (var pendingJunction in pendingJunctions)
            {
                pendingJunction.WaitForExit();
            }

            // Delete the overall temp folder.
            Directory.Delete(tempWebFolder);
        }


        private static void DeleteTempWebFolderAfterIISExpressExits(string tempWebFolder, int iisExpressProcId)
        {
            Assembly assembly = typeof(IISExpressTestLauncher).Assembly;
            string processPath = assembly.Location;

            if (!File.Exists(processPath))
            {
                throw new Exception("Command assembly not found on disk: " + processPath);
            }

            if (Directory.Exists(tempWebFolder))
            {
                string escapedArgs = Program.CleanupTempWebFolderCommand + " \"" + tempWebFolder + "\" " + iisExpressProcId;

                var commandStartInfo = new ProcessStartInfo(processPath, escapedArgs)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(commandStartInfo);
            }
        }



        public void Stop()
        {
            // Kill the process hosting the site, which will in turn result in the second copy of WinIntegrationTesting.exe
            // deleting the temp folder (after it realizes IIS Express has stopped).
            if (this.iisExpressProcess != null)
            {
                try
                {
                    this.iisExpressProcess.Kill();
                }
                catch
                {
                }
                this.iisExpressProcess = null;
            }
        }
    }


    public class StartIISExpressOptions
    {
        private Dictionary<string, string> appSettings = new Dictionary<string, string>();
        private HashSet<string> filesToIgnore = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public StartIISExpressOptions()
        {
        }

        public bool ShowIISExpressConsole { get; set; }

        /// <summary>
        /// If true, after IIS Express is started, the dialog for attaching a debugger will automatically appear.
        /// NOTE: You must check "Manually Choose Debugging Engines" and select "Managed (4.6, 4.5, etc)").
        /// </summary>
        public bool ShowAttachDebuggerDialog { get; set; }

        /// <summary>
        ///  Path to the folder containing csproj file for the web project.
        /// </summary>
        public string WebProjectFolderPath { get; set; }

        /// <summary>
        /// Temporary folder to use for starting the web project. A custom temp folder
        /// </summary>
        public string TempFolderPath { get; set; }

        /// <summary>
        /// Override regular http port in applicationhost.config copy.
        /// </summary>
        public int? HttpPort { get; set; }
        /// <summary>
        /// Override https port in applicatiohost.config copy.
        /// </summary>
        public int? HttpsPort { get; set; }

        /// <summary>
        /// Override host name binding in applicationhost.config copy.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Contents to use for the temp folder applicationhost.config file. By default,
        /// the IISExpressTestLauncher will look for original application host file relative to
        /// web project directory
        /// </summary>
        public string TempApplicationHostConfigContents { get; set; }

        /// <summary>
        /// Contents to use for the temp folder web.config file.
        /// </summary>
        public string TempWebConfigContents { get; set; }


        /// <summary>
        /// Local file names of files to skip processing when setting up the temp web folder.
        /// </summary>
        public ISet<string> FilesToIgnore
        {
            get { return this.filesToIgnore; }
        }

        /// <summary>
        /// Override values in the temp copy of Web.config.
        /// </summary>
        public Dictionary<string, string> AppSettings
        {
            get { return this.appSettings; }
        }


        /// <summary>
        /// Provide a custom callback for editing the temporary web folder before IIS Express is started.
        /// For example, additional configuration files could be saved into the folder.
        /// </summary>
        public SetupTempFolderCallbackSignature SetupTempFolderCallback { get; set; }


        public delegate void SetupTempFolderCallbackSignature(string tempWebFolder);   
    }

}
