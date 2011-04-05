import org.browsermob.proxy.ProxyServer;
import org.openqa.selenium.Proxy;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.remote.DesiredCapabilities;

import java.io.File;

public class Main
{
    public static void main(String[] args) throws Exception
    {
        ProxyServer proxy = new ProxyServer(9090);
        proxy.start();

        Proxy mobProxy = new Proxy().setHttpProxy("localhost:9090");

        DesiredCapabilities caps = new DesiredCapabilities();
        caps.setCapability("proxy", mobProxy);

        FirefoxDriver driver = new FirefoxDriver(caps);
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
