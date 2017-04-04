using IntegrationTestingWithSelenium;
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

                    var someExistingSettingDiv = RemoteJsRef.GetWithCssSelector(webDriver, "#some-existing-setting", maxWait: TimeSpan.FromSeconds(5));
                    var someNewSettingDiv = RemoteJsRef.GetWithCssSelector(webDriver, "#some-new-setting", maxWait: TimeSpan.FromSeconds(1));

                    string someExistingSettingText = someExistingSettingDiv.AsWebElement.Text;
                    Assert.AreEqual("MyModifiedExistingSetting", someExistingSettingText);

                    string someNewSettingText = someNewSettingDiv.AsWebElement.Text;
                    Assert.AreEqual("MyNewSetting", someNewSettingText);

                    var someStoredValue = RemoteJsRef.GetWithScript(webDriver, "return window.someStoredValue;");
                    string someStoredValueJson = someStoredValue.AsJson;
                    dynamic someStoredValueJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(someStoredValueJson);
                    Assert.AreEqual("world", someStoredValueJObject.hello);
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
