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

namespace URLRewrite
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string baseUrl = "http://www.google.com";
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

                // Start Fiddler on port 8877.
                // Register as the system wide proxy.
                Fiddler.FiddlerApplication.Startup(8877, FiddlerCoreStartupFlags.Default);

                // As each HTTP item is downloaded add it to our list.
                // We will export this list later.
                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(sessions);
                    sessions.Add(oS);
                    Monitor.Exit(sessions);
                };

                // Rewrite URLs such that www.google.com becomes "www.google.ie".
                // The browser will still see URLs as www.google.com but the requests
                // will be made to www.google.ie.
                // Requests to other sites will be ignored.
                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    Regex urlMatch = new Regex(@"^(.*\.google\.)com(.*)$");
                    if (urlMatch.IsMatch(oS.url))
                    {
                        oS.url = urlMatch.Replace(oS.url, @"$1ie$2");
                    }
                };

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

                // Export fiddler sessions to HAR file
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
