using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinIntegrationTesting;

namespace WinIntegrationTestingTests
{
    [TestClass]
    public class IISExpressTestLauncherTests
    {
        [TestMethod]
        public void TestStartIISExpress()
        {
            IISExpressTestLauncher siteInstance = null;

            try
            {
                string solutionFolder = VisualStudioHelper.FindSolutionFolderForAssembly(typeof(VisualStudioHelperTests).Assembly);
                string testSiteProjectPath = Path.Combine(solutionFolder, "tests", "SampleIISExpressSite");

                var siteStartOptions = new StartIISExpressOptions
                {
                    WebProjectFolderPath = testSiteProjectPath,
                    HttpPort = 9580,                    
                };

                siteStartOptions.AppSettings["someExistingSetting"] = "MyModifiedExistingSetting";
                siteStartOptions.AppSettings["someNewSetting"] = "MyNewSetting";

                siteInstance = IISExpressTestLauncher.StartIISExpress(siteStartOptions);

                // TODO: Add checks via Selenium for custom settings, someExistingSetting and someNewSetting, being used.
                // text for "#some-existing-setting" == "MyModifiedExistingSetting"
                // text for "#some-new-setting" == "MyNewSetting"

                Assert.IsTrue(true);
            }
            finally
            {
                if (siteInstance != null)
                {
                    siteInstance.Stop();
                }
            }
        }
    }
}
