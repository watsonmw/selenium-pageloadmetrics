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

namespace WaitHTTPIdle
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string baseUrl = "http://www.amazon.com/";
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
                var newSessions = new List<Fiddler.Session>();
                int browserProcessId = -1;

                // As each HTTP item is downloaded add it to our list.
                // We will export this list later.
                Fiddler.FiddlerApplication.AfterSessionComplete += delegate(Fiddler.Session oS)
                {
                    Monitor.Enter(sessions);
                    // Only record HTTP traffic by our browser process
                    if (browserProcessId == oS.LocalProcessID)
                    {
                        sessions.Add(oS);
                    }
                    Monitor.Exit(sessions);
                };

                Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
                {
                    // Record the process id for the first URL we go to,
                    // this is the browser that we lanched with Selenium
                    // Could also do extra verification here, for example
                    // check that it is a child process, of this process.
                    // That requires some heavy Win32 API usage, so I've
                    // skipped it here.
                    if (browserProcessId == -1 && oS.fullUrl == baseUrl)
                    {
                        browserProcessId = oS.LocalProcessID;
                    }

                    // Only record HTTP traffic by our browser process
                    if (browserProcessId == oS.LocalProcessID)
                    {
                        Monitor.Enter(newSessions);
                        newSessions.Add(oS);
                        Monitor.Exit(newSessions);
                    }
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

                // Wait until 3 seconds of HTTP idle (no request being processed for 3 seconds)
                // or a 30 second timeout.
                // 'result' will be false if items are still being downloaded when the timeout
                // occurs.
                bool result = WaitHttpIdle(sessions, 3000, 30000);
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

        /// <summary>
        /// Wait for 'idleTimeMS' seconds of HTTP idle time (no request being processed
        /// for 3 seconds) or a 'timeout second timeout.
        /// Returns 'true' if no items were downloaded for 'idleTimeMS' seconds. 
        /// Returns 'false' if the timeout occurs.
        /// </summary>
        /// <param name="session">The list of sessions to test (passed by reference).</param>
        /// <param name="idleTimeMS">The HTTP traffic idle time in milliseconds to wait.</param>
        /// <param name="timeoutMS">The fallback timeout in milliseconds.</param>
        private static bool WaitHttpIdle
            (List<Fiddler.Session> sessions, int idleTimeMS, int timeoutMS)
        {
            DateTime beginWait = DateTime.Now;
            DateTime lastTrafficTime = beginWait;
            while ((DateTime.Now - beginWait).TotalMilliseconds < timeoutMS)
            {
                Monitor.Enter(sessions);
                for (int i = sessions.Count - 1; i > -1; i--)
                {
                    Session s = sessions[i];
                    
                    if (s.state == SessionStates.Done ||
                        s.state == SessionStates.Aborted) {
                        sessions.RemoveAt(i);
                        lastTrafficTime = DateTime.Now;
                    }

                    // You also may want to ignore certain URLs
                }
                Monitor.Exit(sessions);
                if ((DateTime.Now - lastTrafficTime).TotalMilliseconds > idleTimeMS)
                {
                    return true;
                }
                Thread.Sleep(100);
            }
            return false;
        }
    }
}
