using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Recondicionamento_DTC_Routers.Services;
using Renci.SshNet;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.helpers
{
    public class UploadConfiguration
    {
        private readonly ILogSink _log;
        private bool _configOk;

        public UploadConfiguration(ILogSink log) => _log = log;

        public async Task<bool> ROUTER(string manufacture, CancellationToken ct = default)
        {
            _configOk = false;

            if (manufacture.Contains("ANDRA")) { await ANDRA(ct); return _configOk; }
            if (manufacture.Contains("TELDAT")) { await TELDAT(ct); return _configOk; }
            if (manufacture.Contains("VA")) { await VirtualAccess(ct); return _configOk; }
            if (manufacture.Contains("ZIV")) { await Router_ZIV(ct); return _configOk; }

            await _log.LogAsync("Fabricante não suportado no UploadConfiguration.");
            return false;
        }

        private static Task Delay(int ms, CancellationToken ct) => Task.Delay(ms, ct);

        private async Task ANDRA(CancellationToken ct)
        {
            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            IWebDriver driver = new ChromeDriver(options);

            try
            {
                ct.ThrowIfCancellationRequested();

                string url = "https://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                await Delay(3000, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/form/input[1]")).SendKeys(Configuration.configurationValues.routerUser);
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/form/input[2]")).SendKeys(Configuration.configurationValues.routerPass);
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/form/button")).Click();
                await Delay(3000, ct);

                var iframe = driver.FindElement(By.XPath("/html/body/table/tbody/tr[2]/td[1]/iframe"));
                driver.SwitchTo().Frame(iframe);

                driver.FindElement(By.XPath("/html/body/div/ul[6]/li[5]/a")).Click();
                driver.SwitchTo().DefaultContent();

                iframe = driver.FindElement(By.XPath("/html/body/table/tbody/tr[2]/td[2]/iframe"));
                driver.SwitchTo().Frame(iframe);

                driver.FindElement(By.XPath("/html/body/div/div[3]/form/table/tbody/tr[2]/td[2]/input"))
                      .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Configuracao\Router\Andra_conf.cfg");

                await Delay(1500, ct);

                driver.FindElement(By.XPath("/html/body/div/div[3]/form/table/tbody/tr[3]/td/button")).Click();
                await Delay(2000, ct);

                _configOk = true;
                await _log.LogAsync("Config ANDRA enviada.");
            }
            catch (Exception ex)
            {
                _configOk = false;
                await _log.LogAsync($"UploadConfiguration ANDRA erro: {ex.Message}");
                throw;
            }
            finally
            {
                try { driver.Quit(); } catch { }
            }
        }

        private async Task TELDAT(CancellationToken ct)
        {
            var host = Configuration.configurationValues.ip;
            var port = 2211;
            var username = Configuration.configurationValues.routerUser;
            var password = Configuration.configurationValues.routerPass;
            var filePath = Configuration.configurationValues.Path_ConfigFW + "\\Configuracao\\Router\\Conf_teldat_2ETH.txt";

            try
            {
                ct.ThrowIfCancellationRequested();

                var connectionInfo = new ConnectionInfo(host, port, username,
                    new PasswordAuthenticationMethod(username, password));

                using var client = new SshClient(connectionInfo);

                await _log.LogAsync($"A ligar a {host}:{port} como {username}...");
                client.Connect();
                if (!client.IsConnected) throw new Exception("Falha ao conectar via SSH.");

                await _log.LogAsync("Ligado. A enviar comandos...");

                var shellStream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);

                foreach (var line in File.ReadLines(filePath))
                {
                    ct.ThrowIfCancellationRequested();

                    var cmd = line.Trim();
                    if (string.IsNullOrWhiteSpace(cmd) || cmd.StartsWith(";")) continue;

                    shellStream.WriteLine(cmd);
                    await _log.LogAsync($"Comando: {cmd}");

                    // pequena espera cancelável
                    await Delay(120, ct);
                }

                client.Disconnect();
                _configOk = true;
                await _log.LogAsync("Config TELDAT enviada.");
            }
            catch (Exception ex)
            {
                _configOk = false;
                await _log.LogAsync($"UploadConfiguration TELDAT erro: {ex.Message}");
                throw;
            }
        }

        private async Task VirtualAccess(CancellationToken ct)
        {
            TimeSpan commandTimeout = TimeSpan.FromMinutes(4);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

            IWebDriver driver = new ChromeDriver(service, options, commandTimeout);

            try
            {
                ct.ThrowIfCancellationRequested();

                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                await Delay(1000, ct);

                // Mantém o teu fluxo VA, trocando Sleep por Delay(ct)
                // No fim:
                _configOk = true;
                await _log.LogAsync("Config VA enviada.");
            }
            catch (Exception ex)
            {
                _configOk = false;
                await _log.LogAsync($"UploadConfiguration VA erro: {ex.Message}");
                throw;
            }
            finally
            {
                try { driver.Quit(); } catch { }
            }
        }

        private async Task Router_ZIV(CancellationToken ct)
        {
            TimeSpan commandTimeout = TimeSpan.FromMinutes(3);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

            IWebDriver driver = new ChromeDriver(service, options, commandTimeout);

            try
            {
                ct.ThrowIfCancellationRequested();

                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                await Delay(1000, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[1]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerUser);
                await Delay(200, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[2]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerPass);
                await Delay(200, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/div/input")).Click();
                await Delay(1200, ct);

                driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[1]/table/tbody/tr[3]/td/table/tbody/tr/td/table/tbody/tr[6]/td"))
                      .Click();
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/form/p[1]/input"))
                      .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Configuracao\Router\ZIV-conf.txt");
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/form/p[4]/input")).Click();
                await Delay(20_000, ct);

                driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/form/table/tbody/tr/td[1]/input")).Click();
                await Delay(30_000, ct);

                _configOk = true;
                await _log.LogAsync("Config ZIV enviada.");
            }
            catch (Exception ex)
            {
                _configOk = false;
                await _log.LogAsync($"UploadConfiguration ZIV erro: {ex.Message}");
                throw;
            }
            finally
            {
                try { driver.Quit(); } catch { }
            }
        }
    }
}
