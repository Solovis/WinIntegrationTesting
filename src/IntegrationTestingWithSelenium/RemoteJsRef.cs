using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTestingWithSelenium
{
    public class RemoteJsRef : IRemoteJsRef
    {
        private readonly string id;
        private RemoteWebDriver webDriver;
        private string scriptRef;

        private RemoteJsRef()
        {
            this.id = Guid.NewGuid().ToString("N");
        }


        /// <summary>
        /// Retrieve RemoteJsRef or throw exception if unable too. Script will be used as a function body and should
        /// return a non-null value. The script will be rerurn periodically up to the maxWait time until
        /// a value is returned or time expires.
        /// </summary>       
        public static RemoteJsRef GetWithScript(RemoteWebDriver webDriver, string script, TimeSpan? maxWait = null)
        {
            var objRef = TryGetWithScript(webDriver, script, maxWait);
            if (objRef == null)
            {
                throw new Exception("RemoteJsRef not found for script: " + script);
            }

            return objRef;
        }

        /// <summary>
        /// Retrieve RemoteJsRef if able too (null returned will be returned if retrieval fails). Script will be used as a function body and should
        /// return a non-null value. The script will be rerurn periodically up to the maxWait time until
        /// a value is returned or time expires.
        /// </summary>
        public static RemoteJsRef TryGetWithScript(RemoteWebDriver webDriver, string script, TimeSpan? maxWait = null)
        {
            var objRef = new RemoteJsRef { webDriver = webDriver };
            
            var idJson = ToJson(objRef.id);

            var scriptToExecute =
                "var result = (function(){" + script + "})();\r\n" +
                "if (result === null || typeof result === \"undefined\"){\r\n" +
                "    return false;\r\n" +
                "}\r\n" +
                "if (!window.__TestRemoteJsRef){\r\n" +
                "    window.__TestRemoteJsRef = {};\r\n" +
                "}" +
                "window.__TestRemoteJsRef[" + idJson + "] = result;\r\n" +
                "return true;\r\n";

            objRef.scriptRef = "(window.__TestRemoteJsRef[" + idJson + "])";

            if (RemoteWebDriverHelper.TryWaitUntilScriptIsTrue(webDriver, scriptToExecute, maxWait))
            {
                return objRef;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve RemoteJsRef for CSS selector or throw exception if unable to.
        /// </summary>     
        public static RemoteJsRef GetWithCssSelector(RemoteWebDriver webDriver, string cssSelector, string containerScript = null, TimeSpan? maxWait = null)
        {
            var jsRef = TryGetWithCssSelector(webDriver, cssSelector, containerScript: containerScript, maxWait: maxWait);
            if (jsRef == null)
            {
                throw new Exception("RemoteJsRef not found for css selector: " + cssSelector);
            }

            return jsRef;
        }

        /// <summary>
        /// Retrieve RemoteJsRef for CSS selector or return null if unable to.
        /// </summary>    
        public static RemoteJsRef TryGetWithCssSelector(RemoteWebDriver webDriver, string cssSelector, string containerScript = null, TimeSpan? maxWait = null)
        {            
            string cssSelectorJson = ToJson(cssSelector);
            
            StringBuilder scriptSb = new StringBuilder();          
            if (containerScript != null)
            {
                scriptSb.Append($"var container = {containerScript};");                
            }
            else
            {
                scriptSb.Append($"var container = document;");
            }
            scriptSb.Append($"var match = container.querySelector({cssSelectorJson});");
            scriptSb.Append("return match;");

            string script = scriptSb.ToString();

            return TryGetWithScript(webDriver, script, maxWait);
        }


        /// <summary>
        /// Retrieve RemoteJsRef for JQuery selector or throw exception if unable to (requires JQuery to already be loaded into page).
        /// </summary>
        public static RemoteJsRef GetWithJQuerySelector(RemoteWebDriver webDriver, string sizzle, string containerScript = null, TimeSpan? maxWait = null)
        {
            var objRef = TryGetWithJQuerySelector(webDriver, sizzle, containerScript: containerScript, maxWait: maxWait);
            if (objRef == null)
            {
                throw new Exception("RemoteJsRef not found for JQuery selector: " + sizzle);
            }

            return objRef;
        }

        /// <summary>
        /// Retrieve RemoteJsRef for JQuery selector or return null if unable to (requires JQuery to already be loaded into page).
        /// </summary>
        public static RemoteJsRef TryGetWithJQuerySelector(RemoteWebDriver webDriver, string sizzle, string containerScript = null, TimeSpan? maxWait = null)
        {            
            string escapedSizzle = ToJson(sizzle);

            StringBuilder scriptSb = new StringBuilder();
            scriptSb.Append("var el = $(");
            scriptSb.Append(escapedSizzle);
            if (containerScript != null)
            {
                scriptSb.Append(", ");
                scriptSb.Append(containerScript);
            }
            scriptSb.Append("); ");
            scriptSb.Append("if (el.length) { return el.get(0); } else { return null; }");

            string script = scriptSb.ToString();

            return TryGetWithScript(webDriver, script, maxWait);
        }


        private static string ToJson(object value)
        {
            return Newtonsoft.Json.JsonConvert.ToString(value);
        }

        public RemoteWebDriver WebDriver
        {
            get { return this.webDriver; }
        }

        public string ScriptRef
        {
            get { return this.scriptRef; }
        }       

        public IWebElement AsWebElement
        {
            get
            {
                var result = this.webDriver.ExecuteScript("return " + this.scriptRef);
                if (result is IWebElement)
                {
                    return (IWebElement)result;
                }
                else
                {
                    throw new Exception("RemoteJsRef does not refer to a web element.");
                }
            }
        }

        /// <summary>
        /// Attempts to serialize the reference using JSON.stringify within the browser and retrieves the value.
        /// </summary>
        public string AsJson
        {
            get
            {
                string script = $"var obj = {this.scriptRef}; return JSON.stringify({this.scriptRef});";
                object result = this.webDriver.ExecuteScript(script);
                if (result is string)
                {
                    return (string)result;
                }
                else
                {
                    return null;
                }
            }
        }
        

        public void Dispose()
        {
            try
            {
                if (this.webDriver != null)
                {
                    var scriptToExecute = "try { delete " + this.scriptRef + "; } catch (ex) {}";
                    var result = this.webDriver.ExecuteScript(scriptToExecute);
                    this.webDriver = null;
                    this.scriptRef = null;
                }
            }
            catch
            {
                // We don't really care about an exception thrown here; we just don't want them to halt the test.
            }
        }
    }
}
