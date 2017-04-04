using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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


                var driverService = PhantomJSDriverService.CreateDefaultService();

                // Comment this line to display the console window when running selenium tests.
                driverService.HideCommandPromptWindow = true;
                

                using (var webDriver = new PhantomJSDriver(driverService))
                {
                    webDriver.Navigate().GoToUrl("http://localhost:" + siteStartOptions.HttpPort);
                    
                    // TODO: Add helper that waits for something to be present on page instead of relying on arbitrary wait.
                    Thread.Sleep(3000);

                    string someExistingSettingText = webDriver.FindElementById("some-existing-setting").Text;
                    Assert.AreEqual("MyModifiedExistingSetting", someExistingSettingText);

                    string someNewSettingText = webDriver.FindElementById("some-new-setting").Text;
                    Assert.AreEqual("MyNewSetting", someNewSettingText);
                }                
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
