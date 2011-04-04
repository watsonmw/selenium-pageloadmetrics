import org.openqa.selenium.WebDriver;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.firefox.FirefoxProfile;

import java.io.File;
import java.io.IOException;

/**
 * Created by IntelliJ IDEA.
 * User: Administrator
 * Date: 3/30/11
 * Time: 2:41 PM
 * To change this template use File | Settings | File Templates.
 */
public class Main {
    public static void main(String [] args) {
        WebDriver driver = null;

        System.out.println("Start!");
        try {
            FirefoxProfile profile = new FirefoxProfile();

            try {
                profile.addExtension(new File("c:/firebug-1.6.0.xpi"));
                profile.addExtension(new File("c:/netExport-0.8b9.xpi"));
                profile.addExtension(new File("c:/fireStarter-0.1.a5.xpi"));
            } catch (IOException e) {
                throw new RuntimeException("Could not load required extensions, did you download them to the above location? ", e);
            }
            profile.setPreference("extensions.firebug.currentVersion", "9.99");    // don't display firstrun
            profile.setPreference("extensions.firebug.onByDefault", true);
            profile.setPreference("extensions.firebug.net.enableSites", true);

            profile.setPreference("extensions.firebug.netexport.alwaysEnableAutoExport", true);
            profile.setPreference("extensions.firebug.netexport.autoExportToFile", true);
            profile.setPreference("extensions.firebug.netexport.autoExportToServer", false);
            profile.setPreference("extensions.firebug.netexport.showPreview", false); // Don't preview.
            // Don't ask for confirmation. This seemed to crash FF / Se when set to automatically run.
            profile.setPreference("extensions.firebug.netexport.sendToConfirmation", false);
            profile.setPreference("extensions.firebug.netexport.pageLoadedTimeout", 1500);
            // Will break the export for unknown reason if set
            // profile.setPreference("extensions.firebug.netexport.defaultLogDir", "c:/");

            driver = new FirefoxDriver(profile);

            // After each get() you will see a HAR file output in the <profileDir>/firebug/netexport/logs/
            // By default on windows this is: C:\Documents and Settings\Administrator\Local Settings\Temp\anonymous<randomId>webdriver-profile\firebug\netexport\logs
            driver.get("http://google.com");
            driver.get("http://webmetrics.com");
        }
        catch (Exception ex) {
            System.out.println("Exception: " + ex.getMessage());
        }
        finally {
            if (driver != null) {
                driver.quit();
            }
        }
        System.out.println("Done!");
    }
}

