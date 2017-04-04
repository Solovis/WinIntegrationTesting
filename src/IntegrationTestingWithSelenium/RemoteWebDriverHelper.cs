using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTestingWithSelenium
{
    public static class RemoteWebDriverHelper
    {
        /// <summary>
        /// Waits until a given script returns true. Will return false if the script does not return true within the given wait time.
        /// </summary>
        /// <param name="webDriver"></param>
        /// <param name="script">Script to run</param>
        /// <param name="maxWaitForTrue">Maximum time to run the script before returning false</param>
        public static bool TryWaitUntilScriptIsTrue(this RemoteWebDriver webDriver, string script, TimeSpan? maxWaitForTrue = null)
        {
            int waitTimeBetweenElementSearches = 50;
            int totalSleepTime = 0;
            int totalSleepTimeAllowed = maxWaitForTrue.HasValue ? (int)maxWaitForTrue.Value.TotalMilliseconds : 0;

            var scriptToExecute =
                "try{\r\n" +
                "    var result = (function(){" + script + "})();\r\n" +
                "    if (result) {\r\n" +
                "        return true;\r\n" +
                "    }\r\n" +
                "    return false;\r\n" +
                "} catch(ex) {\r\n" +
                "    return false;\r\n" +
                "}";

            object result = webDriver.ExecuteScript(scriptToExecute);

            if ((bool)result)
            {
                return true;
            }

            while (totalSleepTime < totalSleepTimeAllowed)
            {
                result = webDriver.ExecuteScript(scriptToExecute);
                if ((bool)result)
                {
                    return true;
                }

                Thread.Sleep(waitTimeBetweenElementSearches);
                totalSleepTime += waitTimeBetweenElementSearches;
            }

            return false;
        }
    }
}
