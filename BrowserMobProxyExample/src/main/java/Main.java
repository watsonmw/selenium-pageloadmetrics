import org.browsermob.proxy.ProxyServer;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.firefox.FirefoxProfile;
import java.io.File;

public class Main
{
    public static void main(String[] args) throws Exception
    {
        ProxyServer proxy = new ProxyServer(9090);
        proxy.start();
        FirefoxProfile profile = new FirefoxProfile();
        profile.setPreference("network.proxy.type", 1);
        profile.setPreference("network.proxy.http", "localhost");
        profile.setPreference("network.proxy.http_port", 9090);
        profile.setPreference("network.proxy.ssl", "localhost");
        profile.setPreference("network.proxy.ssl_port", 9090);
        FirefoxDriver driver = new FirefoxDriver(profile);
        proxy.newHar("Yahoo");
        driver.get("http://yahoo.com");
        proxy.endPage();
        proxy.newPage("CNN");
        driver.get("http://cnn.com");
        proxy.endPage();
        proxy.getHar().writeTo(new File("test.har"));
        driver.close();
        proxy.stop();
    }
}

