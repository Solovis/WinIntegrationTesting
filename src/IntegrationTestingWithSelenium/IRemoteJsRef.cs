using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTestingWithSelenium
{
    /// <summary>
    /// Reference to a specific Javascript object within a RemoteWebDriver instance.
    /// </summary>
    public interface IRemoteJsRef : IDisposable
    {
        RemoteWebDriver WebDriver { get; }

        string ScriptRef { get; }
    }
}
