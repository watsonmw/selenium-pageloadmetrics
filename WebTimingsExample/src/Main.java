import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.chrome.ChromeDriver;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.firefox.FirefoxProfile;
import org.openqa.selenium.ie.InternetExplorerDriver;

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
            driver = new ChromeDriver();
            // A "base url", used by selenium to resolve relative URLs
            String baseUrl = "http://www.google.com";
            driver.get(baseUrl);
            JavascriptExecutor js = (JavascriptExecutor)driver;
            Long loadEventEnt = (Long) js.executeScript("return window.performance.timing.loadEventEnd;");
            Long navigationStart = (Long) js.executeScript("return window.performance.timing.navigationStart;");
            System.out.println("Page Load Time = " + (loadEventEnt - navigationStart));
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

