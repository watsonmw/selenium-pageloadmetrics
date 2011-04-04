using System;
using System.Collections.Generic;
using Fiddler;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using Selenium;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Reflection;

namespace HARExport
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string baseUrl = "http://www.webmetrics.com";
                if (args.Length >= 1)
                {
                    baseUrl = args[0];
                }

                string filename;
                if (args.Length >= 2)
                {
                    filename = args[1];
                }
                else
                {
                    Uri uri = new Uri(baseUrl);
                    filename = uri.Host.Replace(".", "-") + @".har";
                }

                // Uncomment to enable SSL support
                // Fiddler.CONFIG.IgnoreServerCertErrors = true;
                // FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

                // Varibale to store the list of items downloaded
                var sessions = new List<Fiddler.Session>();

                // As each HTTP item is downloaded add it to our list.
                // We will export this list later.
                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(sessions);
                    sessions.Add(oS);
                    Monitor.Exit(sessions);
                };

                // Return HTTP response code 200 (OK) to the the browser for any
                // external website URLs requested.
                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    if (!new Regex(baseUrl).IsMatch(oS.fullUrl))
                    {
                        // Create a dummy response and never sent the HTTP request to
                        // the server.
                        oS.utilCreateResponseAndBypassServer();
                        oS.responseCode = 200;
                    }
                    // We will still add this item to the HAR file, just to show that
                    // the request was made correctly by the browser.  This is optional,
                    // if you don't want to record the request in the HAR file, just
                    // don't add the item to the list off of sessions above.
                };

                // Start Fiddler on port 8877.
                // Register as the system wide proxy.
                Fiddler.FiddlerApplication.Startup(8877, FiddlerCoreStartupFlags.Default);

                /////////////////////////////////////////////////
                // Begin selenium test
                /////////////////////////////////////////////////

                var webDriver = new OpenQA.Selenium.Firefox.FirefoxDriver();
                var selenium = new Selenium.WebDriverBackedSelenium(webDriver, baseUrl);

                selenium.Start();
                selenium.Open(baseUrl);
                selenium.WaitForPageToLoad("30000");
                selenium.Stop();

                /////////////////////////////////////////////////
                // End selenium test
                /////////////////////////////////////////////////

                // Load the HAR file exporter (this only has to be done once per process).
                // The following DLL was downloaded from:
                // https://www.fiddler2.com/dl/FiddlerCore-BasicFormats.zip
                // It is currently only loadable with FiddlerCode 2.2.9.9.
                String path = Path.Combine(Path.GetDirectoryName
                    (Assembly.GetExecutingAssembly().Location), @"FiddlerCore-BasicFormats.dll");
                FiddlerApplication.oTranscoders.ImportTranscoders(path);

                // Export recorded HTTP transactions to HAR file
                Monitor.Enter(sessions);
                var oExportOptions = new Dictionary<string, object>();
                oExportOptions.Add("Filename", filename);
                bool fiddler = Fiddler.FiddlerApplication.DoExport("HTTPArchive v1.2", sessions.ToArray(), oExportOptions, null);
                sessions.Clear();
                Monitor.Exit(sessions);
            }
            finally
            {
                // Shutdown fiddler, this removes Fiddler as the system proxy. 
                Fiddler.FiddlerApplication.Shutdown();
                Thread.Sleep(500);
            }
        }
    }
}
