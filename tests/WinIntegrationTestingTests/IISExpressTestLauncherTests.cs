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
        //[TestMethod]
        // TODO: Enable as an actual test after adding way to stop the site programmatically
        // and using Selenium to validate some customized app settings.
        public void TestStartIISExpress()
        {
            string solutionFolder = VisualStudioHelper.FindSolutionFolderForAssembly(typeof(VisualStudioHelperTests).Assembly);
            string testSiteProjectPath = Path.Combine(solutionFolder, "tests", "SampleIISExpressSite");

            var siteInstance = IISExpressTestLauncher.StartIISExpress(
                new StartIISExpressOptions
                {
                    WebProjectFolderPath = testSiteProjectPath,
                    HttpPort = 9580                   
                });

            Assert.IsTrue(true);
        }
    }
}
