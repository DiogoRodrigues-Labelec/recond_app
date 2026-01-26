using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.helpers
{
    public sealed class CircutorDtcHelper
    {
        private readonly ILogSink _log;

        public CircutorDtcHelper(ILogSink log) => _log = log;

        public async Task<(string Id, string Firmware)> GetSnapshotAsync(string ip, int port, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var driver = CreateDriver();
            var wait = CreateWait(driver, 25);

            string baseUrl = $"http://{ip}:{port}/";
            driver.Navigate().GoToUrl(baseUrl);

            await _log.LogAsync($"[CIRCUTOR] Open: {baseUrl}");

            EnsureLoggedIn(driver, wait, user: "admin", pass: "admin");

            // Main page has Identifier / Version somewhere (as per your print)
            wait.Until(d => d.PageSource.Length > 1500);

            string src = driver.PageSource ?? "";
            string bodyText = SafeText(driver);

            string id = FirstMatch(bodyText, @"Identifier:\s*([A-Za-z0-9_]+)");
            if (string.IsNullOrWhiteSpace(id))
                id = FirstMatch(src, @"Identifier:\s*([A-Za-z0-9_]+)");

            string ver = FirstMatch(bodyText, @"Version:\s*([0-9]+\.[0-9]+\.[0-9A-Za-z]+)");
            if (string.IsNullOrWhiteSpace(ver))
                ver = FirstMatch(src, @"Version:\s*([0-9]+\.[0-9]+\.[0-9A-Za-z]+)");

            id = (id ?? "").Trim();
            ver = (ver ?? "").Trim();

            await _log.LogAsync($"[CIRCUTOR] Snapshot -> ID='{id}' FW='{ver}'");

            return (id, ver);
        }

        public async Task UpgradeFirmwareAsync(string ip, int port, string tarPath, int waitRebootSeconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(tarPath))
                throw new ArgumentException(nameof(tarPath));

            if (!File.Exists(tarPath))
                throw new FileNotFoundException("Firmware .tar não encontrado.", tarPath);

            using var driver = CreateDriver();
            var wait = CreateWait(driver, 30);

            string baseUrl = $"http://{ip}:{port}/";
            driver.Navigate().GoToUrl(baseUrl);

            await _log.LogAsync($"[CIRCUTOR] Open: {baseUrl}");
            await _log.LogAsync($"[CIRCUTOR] Upgrade TAR: {tarPath}");

            EnsureLoggedIn(driver, wait, user: "admin", pass: "admin");

            // Click the left menu button "Update" (your UI: input type=button value=Update)
            ClickUpdateMenu(driver, wait);

            // Wait until update page shows file input
            var fileInput = wait.Until(d => TryFind(d, By.CssSelector("input[type='file']")));
            if (fileInput == null)
                throw new Exception("[CIRCUTOR] Update page: não encontrei input[type=file].");

            fileInput.SendKeys(tarPath);
            await _log.LogAsync("[CIRCUTOR] TAR colocado no input[type=file].");

            // Click Send
            var sendBtn =
                TryFind(driver, By.CssSelector("input[type='submit'][value='Send']")) ??
                TryFind(driver, By.XPath("//input[@type='submit' and contains(@value,'Send')]")) ??
                TryFind(driver, By.XPath("//button[normalize-space()='Send' or contains(.,'Send')]")) ??
                TryFind(driver, By.CssSelector("input[type='submit']"));

            if (sendBtn == null)
                throw new Exception("[CIRCUTOR] Update page: não encontrei botão 'Send'.");

            WaitClickable(wait, sendBtn);
            SafeClick(driver, sendBtn);

            await _log.LogAsync("[CIRCUTOR] Send clicado. A aguardar reboot/HTTP up...");

            // depois de sendBtn.Click();
            await _log.LogAsync("[CIRCUTOR] Send feito. Aguardar ~90s para o update aplicar...");

            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            // só depois esperar o reboot / serviço voltar
            await _log.LogAsync("[CIRCUTOR] A aguardar voltar a responder por HTTP...");
            await WaitHttpUpAsync(baseUrl, timeoutSeconds: 600, ct);

            // device reboots: wait HTTP up
            await WaitHttpUpAsync(baseUrl, waitRebootSeconds, ct);

            await _log.LogAsync("[CIRCUTOR] HTTP voltou a responder após upgrade.");
        }

        // ------------------------- internal -------------------------

        private static IWebDriver CreateDriver()
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            // options.AddArgument("--headless=new"); // se quiseres headless, mas tu queres ver a janela

            return new ChromeDriver(service, options);
        }

        private static WebDriverWait CreateWait(IWebDriver driver, int seconds)
            => new WebDriverWait(new SystemClock(), driver, TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(200));

        private void EnsureLoggedIn(IWebDriver driver, WebDriverWait wait, string user, string pass)
        {
            // If password box exists -> login page
            var passBox = TryFind(driver, By.CssSelector("input[type='password']"));
            if (passBox == null)
            {
                // already logged in
                WaitMainLoaded(driver, wait);
                return;
            }

            var form = passBox.FindElement(By.XPath("./ancestor::form[1]"));

            var userBox =
                TryFind(form, By.CssSelector("input[type='text']")) ??
                TryFind(form, By.CssSelector("input[name*='user'], input[id*='user']")) ??
                TryFind(form, By.XPath(".//input[not(@type) or @type='text']"));

            if (userBox == null)
                throw new Exception("[CIRCUTOR] Login: não encontrei textbox de user.");

            wait.Until(_ =>
            {
                try { return userBox.Displayed && userBox.Enabled && passBox.Displayed && passBox.Enabled; }
                catch { return false; }
            });

            userBox.Clear();
            userBox.SendKeys(user);

            passBox.Clear();
            passBox.SendKeys(pass);

            var btn =
                TryFind(form, By.CssSelector("input[type='submit']")) ??
                TryFind(form, By.CssSelector("button[type='submit']")) ??
                TryFind(form, By.XPath(".//button[contains(.,'Login') or contains(.,'log in') or contains(.,'Entrar')]")) ??
                TryFind(form, By.XPath(".//input[contains(@value,'Login') or contains(@value,'Entrar')]"));

            if (btn != null)
            {
                WaitClickable(wait, btn);
                SafeClick(driver, btn);
            }
            else
            {
                passBox.SendKeys(OpenQA.Selenium.Keys.Enter);
            }

            // IMPORTANT: don’t “return too fast”
            WaitMainLoaded(driver, wait);
        }

        private static void WaitMainLoaded(IWebDriver driver, WebDriverWait wait)
        {
            // Wait until the main “Update” menu button exists
            wait.Until(d =>
            {
                try
                {
                    var updateBtn = TryFind(d, By.XPath("//input[@type='button' and normalize-space(@value)='Update']"));
                    if (updateBtn != null) return true;

                    // fallback: page grew
                    return d.PageSource != null && d.PageSource.Length > 2000;
                }
                catch { return false; }
            });
        }

        private void ClickUpdateMenu(IWebDriver driver, WebDriverWait wait)
        {
            // Your UI: <input type="button" class="BMA" value="Update" onclick="Request('Update')">
            var updateBtn =
                TryFind(driver, By.XPath("//input[@type='button' and normalize-space(@value)='Update']")) ??
                TryFind(driver, By.CssSelector("input[type='button'][value='Update']")) ??
                TryFind(driver, By.XPath("//td[contains(@class,'RMA')]//input[@type='button' and contains(@value,'Update')]"));

            if (updateBtn == null)
                throw new Exception("[CIRCUTOR] Não encontrei o botão menu 'Update' (input value='Update').");

            WaitClickable(wait, updateBtn);
            SafeClick(driver, updateBtn);

            // Wait update page: file input appears OR text hints
            wait.Until(d =>
            {
                try
                {
                    if (TryFind(d, By.CssSelector("input[type='file']")) != null) return true;
                    var src = d.PageSource ?? "";
                    return src.IndexOf("Send", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                catch { return false; }
            });

            _ = _log.LogAsync("[CIRCUTOR] Entrei no ecrã Update.");
        }

        private static IWebElement TryFind(ISearchContext ctx, By by)
        {
            try { return ctx.FindElement(by); } catch { return null; }
        }

        private static void WaitClickable(WebDriverWait wait, IWebElement el)
        {
            wait.Until(_ =>
            {
                try { return el != null && el.Displayed && el.Enabled; }
                catch { return false; }
            });
        }

        private static void SafeClick(IWebDriver driver, IWebElement el)
        {
            try
            {
                el.Click();
            }
            catch
            {
                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el);
                }
                catch
                {
                    // last resort: focus + enter
                    el.SendKeys(OpenQA.Selenium.Keys.Enter);
                }
            }
        }

        private static string SafeText(IWebDriver driver)
        {
            try { return driver.FindElement(By.TagName("body"))?.Text ?? ""; }
            catch { return ""; }
        }

        private static string FirstMatch(string text, string pattern)
        {
            try
            {
                var m = Regex.Match(text ?? "", pattern, RegexOptions.IgnoreCase);
                return m.Success ? m.Groups[1].Value : "";
            }
            catch { return ""; }
        }

        private static async Task WaitHttpUpAsync(string url, int timeoutSeconds, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            // give it a moment to actually reboot
            await Task.Delay(5000, ct);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var resp = await http.GetAsync(url, ct);
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                        return;
                }
                catch { }

                await Task.Delay(2000, ct);
            }

            throw new Exception($"[CIRCUTOR] Timeout: não voltou por HTTP em {timeoutSeconds}s.");
        }
    }
}
