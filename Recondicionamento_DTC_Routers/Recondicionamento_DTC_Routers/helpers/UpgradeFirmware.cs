using FluentFTP;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Recondicionamento_DTC_Routers.Services;
using Renci.SshNet;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.helpers
{
    public sealed class UpgradeFirmware
    {
        private readonly ILogSink _log;

        public UpgradeFirmware(ILogSink log) => _log = log;

        public async Task ROUTER(string manufacture, CancellationToken ct = default)
        {
            if (manufacture.Contains("ANDRA")) { await ANDRA(ct); return; }
            if (manufacture.Contains("TELDAT")) { await TELDAT(ct); return; }
            if (manufacture.Contains("VA")) { await VirtualAccess(ct); return; }
            if (manufacture.Contains("ZIV")) { await Router_ZIV(ct); return; }

            await _log.LogAsync("Fabricante não suportado no UpgradeFirmware.");
        }

        private static Task Delay(int ms, CancellationToken ct) => Task.Delay(ms, ct);

        private async Task ANDRA(CancellationToken ct)
        {
            ChromeOptions options = new ChromeOptions();

            // Ignora erros de certificado SSL
            options.AddArgument("--ignore-certificate-errors");

            // (Opcional) Executa em modo headless
            // options.AddArgument("--headless");

            IWebDriver driver = new ChromeDriver(options);



            try
            {
                string url = "https://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(4000);

                driver.FindElement(By.XPath("/html/body/div[2]/form/input[1]")) //user
                      .SendKeys(Configuration.configurationValues.routerUser);
                Thread.Sleep(1000);
                driver.FindElement(By.XPath("/html/body/div[2]/form/input[2]")) //pass
                      .SendKeys(Configuration.configurationValues.routerPass);
                Thread.Sleep(1000);
                driver.FindElement(By.XPath("/html/body/div[2]/form/button")) // login
                      .Click();
                Thread.Sleep(4000);



                var iframe = driver.FindElement(By.XPath("/html/body/table/tbody/tr[2]/td[1]/iframe"));
                driver.SwitchTo().Frame(iframe);




                // Localiza o botão que ativa o menu ao passar o rato
                driver.FindElement(By.XPath("/html/body/div/ul[6]/li[3]/a")).Click();//System

                driver.SwitchTo().DefaultContent();
                iframe = driver.FindElement(By.XPath("/html/body/table/tbody/tr[2]/td[2]/iframe"));
                driver.SwitchTo().Frame(iframe);


                driver.FindElement(By.XPath("/html/body/div/div/form/table/tbody/tr[2]/td[2]/input"))
                  .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\Andra - 1.9.24.6\cumulative.apk"); // ok

                Thread.Sleep(1000);

                driver.FindElement(By.XPath("/html/body/div/div/form/table/tbody/tr[3]/td/button"))
                   .Click();

                Thread.Sleep(1000);


                driver.FindElement(By.XPath("/html/body/div/div/form/table/tbody/tr[3]/td/button"))
                    .Click();



            }
            catch (Exception ex)
            {

                await _log.LogAsync($"Erro: {ex.Message}");

            }
            finally
            {
                await _log.LogAsync($"Upgrade efetuado com sucesso\nA Reeniciar...");
                Thread.Sleep(10000);
                driver.Quit();
            }
        }

        private async Task TELDAT(CancellationToken ct)
        {
            string baseDir = Path.Combine(
                Configuration.configurationValues.Path_ConfigFW,
                @"Firmware\Routers\Teldat - 11.01.10.45.03\rs3g_1101104503_standard"
            );

            string localBios = Path.Combine(baseDir, "bmips34k.bin");
            string localCit = Path.Combine(baseDir, "rs3g.bin");
            string localFw01c = Path.Combine(baseDir, "fw00001c.bfw");
            string localFw01d = Path.Combine(baseDir, "fw00001d.bfw");

            if (!File.Exists(localBios)) throw new FileNotFoundException("BIOS não encontrado", localBios);
            if (!File.Exists(localCit)) throw new FileNotFoundException("CIT não encontrado", localCit);

            string ftpIp = Configuration.configurationValues.ip;
            int ftpPort = 21;
            string ftpUser = Configuration.configurationValues.routerUser;
            string ftpPass = Configuration.configurationValues.routerPass;

            var cfg = new FtpConfig
            {
                ConnectTimeout = 20000,
                ReadTimeout = 140000,
                DataConnectionConnectTimeout = 20000,
                DataConnectionReadTimeout = 140000,

                // força ACTIVE/PORT
                DataConnectionType = FtpDataConnectionType.PORT,

                SocketKeepAlive = true,
                NoopInterval = 10,

                // evita retries internos
                RetryAttempts = 0,
            };

            using var client = new FtpClient(ftpIp, ftpUser, ftpPass, ftpPort, cfg);

            try
            {
                ct.ThrowIfCancellationRequested();
                await _log.LogAsync($"[TELDAT] FTP connect {ftpIp}:{ftpPort} user='{ftpUser}'");

                client.Connect();
                await _log.LogAsync("[TELDAT] Connected");

                // bin
                await ExecCmdLogAsync(client, "TYPE I", "TYPE", ct);

                // não renomear automaticamente
                await ExecCmdLogAsync(client, "SITE CHECK RENAME OFF", "CHECK RENAME", ct);

                // (opcional) apagar buffer após savebuffer para não ficar “preso”
                // se isto falhar, ignoramos
                try { await ExecCmdLogAsync(client, "SITE CHECK DELETE ON", "CHECK DELETE", ct); } catch { }

                // Nome do CIT ativo (para sobrescrever e não ficar com 2 BINs)
                string remoteCitName = "appcode1.bin";
                try
                {
                    var rep = client.Execute("SITE GETAPPNAME");
                    string msg = ((rep.InfoMessages ?? "") + " " + (rep.Message ?? "")).Trim();
                    var m = Regex.Match(msg, @"([A-Za-z0-9_\-]+\.bin)\b", RegexOptions.IgnoreCase);
                    if (m.Success) remoteCitName = m.Groups[1].Value;
                    await _log.LogAsync($"[TELDAT] SITE GETAPPNAME -> vou gravar CIT como '{remoteCitName}'");
                }
                catch (Exception ex)
                {
                    await LogExceptionChainAsync("[TELDAT] GETAPPNAME", ex);
                    await _log.LogAsync($"[TELDAT] GETAPPNAME falhou; vou usar '{remoteCitName}'");
                }

                // ===== BIOS =====
                await SendFileWithSavebufferAsync(client, localBios, "bmips34k.bin", "BIOS", ct);

                // ===== Firmwares (se quiseres mesmo enviar) =====
                // IMPORTANTE: nome remoto = só filename, sem "/"
                if (File.Exists(localFw01c))
                    await SendFileWithSavebufferAsync(client, localFw01c, "fw00001c.bfw", "FW01C", ct);

                if (File.Exists(localFw01d))
                    await SendFileWithSavebufferAsync(client, localFw01d, "fw00001d.bfw", "FW01D", ct);

                // ===== CIT =====
                // para evitar 2 BINs e falta de espaço: sobrescreve o ativo
                await SendFileWithSavebufferAsync(client, localCit, remoteCitName, "CIT", ct);

                // opcional
                try { await ExecCmdLogAsync(client, "SITE COHERENCE", "COHERENCE", ct); } catch { }

                await ExecCmdLogAsync(client, "SITE RELOAD ON", "RELOAD", ct);

                try { client.Disconnect(); } catch { }
                await _log.LogAsync("[TELDAT] FTP Disconnect");
                await _log.LogAsync("[TELDAT] Transferência concluída.");
            }
            catch (Exception ex)
            {
                await LogExceptionChainAsync("[TELDAT] FAIL", ex);
                throw;
            }
        }

        private async Task SendFileWithSavebufferAsync(FtpClient client, string localPath, string remoteFileName, string tag, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // garante nome remoto LIMPO (sem paths)
            remoteFileName = Path.GetFileName(remoteFileName);

            // por defeito (procedimento normal)
            try { await ExecCmdLogAsync(client, "SITE DIRECT OFF", $"{tag} DIRECT OFF", ct); } catch { }

            await _log.LogAsync($"[TELDAT] A enviar {tag} -> {remoteFileName}");
            try
            {
                await UploadNoResumeAsync(client, localPath, remoteFileName, ct);
                await _log.LogAsync($"[TELDAT] {tag}: PUT OK -> {remoteFileName}");
            }
            catch (Exception ex)
            {
                await _log.LogAsync($"[TELDAT] {tag}: PUT FAIL -> {remoteFileName}");
                await LogExceptionChainAsync($"[TELDAT] {tag} UPLOAD", ex);

                // 1 retry simples com DIRECT ON (manual)
                await _log.LogAsync($"[TELDAT] {tag}: retry 1x com SITE DIRECT ON...");
                await ExecCmdLogAsync(client, "SITE DIRECT ON", $"{tag} DIRECT ON", ct);

                try
                {
                    await UploadNoResumeAsync(client, localPath, remoteFileName, ct);
                    await _log.LogAsync($"[TELDAT] {tag}: PUT OK (DIRECT ON) -> {remoteFileName}");
                }
                catch (Exception ex2)
                {
                    await _log.LogAsync($"[TELDAT] {tag}: PUT FAIL (DIRECT ON) -> {remoteFileName}");
                    await LogExceptionChainAsync($"[TELDAT] {tag} UPLOAD DIRECTON", ex2);
                    throw;
                }
                finally
                {
                    try { await ExecCmdLogAsync(client, "SITE DIRECT OFF", $"{tag} DIRECT OFF", ct); } catch { }
                }
            }

            await _log.LogAsync($"[TELDAT] {tag}: a executar SITE SAVEBUFFER...");
            await ExecCmdLogAsync(client, "SITE SAVEBUFFER", $"{tag} SAVEBUFFER", ct);

            // sem loops / sem STATBUFFER
            await Task.Delay(4000, ct);
        }

        private async Task UploadNoResumeAsync(FtpClient client, string localPath, string remoteFileName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Abrir stream local
            using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Abrir stream remoto (STOR) - sem “resume”
            using var rs = client.OpenWrite(remoteFileName, FtpDataType.Binary);

            // Copy com cancelamento
            byte[] buf = new byte[128 * 1024];
            int read;
            while ((read = await fs.ReadAsync(buf, 0, buf.Length, ct)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                rs.Write(buf, 0, read); // FluentFTP stream é sync
            }

            rs.Flush();
        }

        private async Task<FtpReply> ExecCmdLogAsync(FtpClient client, string cmd, string tag, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var rep = client.Execute(cmd);

            string msg = $"{tag}: ({rep.Code}) {rep.Message}".Trim();
            if (!string.IsNullOrWhiteSpace(rep.InfoMessages))
                msg += "\n" + rep.InfoMessages.TrimEnd();

            await _log.LogAsync("[TELDAT] " + msg);
            return rep;
        }

        private async Task LogExceptionChainAsync(string prefix, Exception ex)
        {
            if (ex == null) return;

            await _log.LogAsync($"{prefix}: {ex.GetType().Name}: {ex.Message}");

            if (ex is AggregateException aex)
            {
                var flat = aex.Flatten();
                int i = 0;
                foreach (var inner in flat.InnerExceptions)
                {
                    i++;
                    await _log.LogAsync($"{prefix}: INNER_AGG[{i}] {inner.GetType().Name}: {inner.Message}");
                }
            }

            int depth = 0;
            var cur = ex.InnerException;
            while (cur != null && depth < 8)
            {
                depth++;
                await _log.LogAsync($"{prefix}: INNER[{depth}] {cur.GetType().Name}: {cur.Message}");
                cur = cur.InnerException;
            }

            await _log.LogAsync($"{prefix}: STACK: {ex.StackTrace}");
        }


        private async Task VirtualAccess(CancellationToken ct)
        {
            TimeSpan commandTimeout = TimeSpan.FromMinutes(4);

            // Inicia o serviço do ChromeDriver
            var service = ChromeDriverService.CreateDefaultService();
            service.Start(); // Garante que o serviço está ativo
            service.HideCommandPromptWindow = true;

            // Define opções do Chrome
            ChromeOptions options = new ChromeOptions();

            // Cria o driver com timeout aumentado

            IWebDriver driver = new OpenQA.Selenium.Remote.RemoteWebDriver(
                service.ServiceUrl,
                options.ToCapabilities(),
                commandTimeout
            );


            driver.Manage().Window.Maximize();

            bool hmi_novo = false;


            try
            {
                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(1000);

                var elementos = driver.FindElements(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input"));
                if (!elementos.Any())
                {
                    hmi_novo = true;
                }
                if (!hmi_novo)
                {

                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input")) //user
                      .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(1000);
                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[2]/div/input")) //pass
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(1000);
                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[2]/input[1]")) // login
                          .Click();
                    Thread.Sleep(1000);
                    //driver.FindElement(By.XPath("/html/body/header/div/div/ul/li[2]/ul/li[3]/a")) 
                    //     .Click();
                    //Thread.Sleep(10000);

                    try
                    {
                        // Tenta esperar pelo alerta
                        //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.AlertIsPresent());

                        // Se chegou aqui, alerta apareceu
                        IAlert alerta = driver.SwitchTo().Alert();

                        Thread.Sleep(1000);
                        alerta.Accept(); // Clica em OK
                        Thread.Sleep(1000);
                        driver.SwitchTo().DefaultContent();

                    }
                    catch (Exception ex)
                    {

                    }

                    // Localiza o botão que ativa o menu ao passar o rato
                    IWebElement botao = driver.FindElement(By.XPath("/html/body/header/div/div/ul/li[2]"));//System

                    //// Cria uma instância de Actions
                    Actions actions = new Actions(driver);

                    //// Move o rato para o botão (hover)
                    actions.MoveToElement(botao).Perform();
                    Thread.Sleep(1000);

                    //// Agora o menu deve estar visível
                    //// Podes continuar a interagir com os elementos do menu, por exemplo:
                    driver.FindElement(By.XPath("/html/body/header/div/div/ul/li[2]/ul/li[2]/a"))
                        .Click();

                    Thread.Sleep(5000);

                    //driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[4]/td[5]/div"))
                    //    .Click();


                    // Verifica se o elemento existe
                    var elementos1 = driver.FindElements(By.XPath("/html/body/div/form/table/tbody/tr[2]/td[5]/div/div/div[2]/input"));



                    //IWebElement elemento = driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[4]/td[5]/div/input"));

                    // Verifica se está ativo (habilitado)
                    if (elementos1.Any())
                    {


                        driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[2]/td[5]/div/div/div[2]/input"))
                        .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\Westermo - GW2124p - 25.03.43.000\ORK-25.03.43.000.image"); // ok
                                                                                                                                                                   //
                        Thread.Sleep(1 * 60000);

                        driver.FindElement(By.XPath("/html/body/div/div[1]/button[2]"))
                           .Click();



                        Thread.Sleep(1 * 60000);
                    }
                    else
                    {
                        driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[3]/td[5]/div/div/div[2]/input"))
                           .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\Westermo - GW2124p - 25.03.43.000\ORK-25.03.43.000.image"); // ok

                        Thread.Sleep(1 * 60000);

                        driver.FindElement(By.XPath("/html/body/div/div[1]/button[2]"))
                           .Click();



                        Thread.Sleep(1 * 60000);
                    }


                    Thread.Sleep(60000);


                    await _log.LogAsync($"Update Realizado com sucesso");
                }
                else
                {
                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[1]/input")) //user
                      .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(1000);
                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[2]/input")) //pass
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(1000);
                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[2]/input")) // login
                          .Click();
                    Thread.Sleep(1000);
                    //driver.FindElement(By.XPath("/html/body/header/div/div/ul/li[2]/ul/li[3]/a")) 
                    //     .Click();
                    //Thread.Sleep(10000);

                    try
                    {
                        // Tenta esperar pelo alerta
                        //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.AlertIsPresent());

                        // Se chegou aqui, alerta apareceu
                        IAlert alerta = driver.SwitchTo().Alert();

                        Thread.Sleep(1000);
                        alerta.Accept(); // Clica em OK
                        Thread.Sleep(1000);
                        driver.SwitchTo().DefaultContent();

                    }
                    catch (Exception ex)
                    {

                    }


                    //// Podes continuar a interagir com os elementos do menu, por exemplo:
                    driver.FindElement(By.XPath("/html/body/div/div[1]/ul/li[2]/a"))//system
                        .Click();

                    Thread.Sleep(1000);

                    driver.FindElement(By.XPath("/html/body/div/div[1]/ul/li[2]/ul/li[2]/a"))//Flash operations
                       .Click();

                    //driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[4]/td[5]/div"))
                    //    .Click();


                    // Verifica se o elemento existe
                    var elementos1 = driver.FindElements(By.XPath("/html/body/div/div[2]/div[2]/div/form/table/tbody/tr[3]/td[5]/div/span"));



                    //IWebElement elemento = driver.FindElement(By.XPath("/html/body/div/form/table/tbody/tr[4]/td[5]/div/input"));

                    // Verifica se está ativo (habilitado)
                    if (elementos1.Any())
                    {


                        driver.FindElement(By.XPath("/html/body/div/div[2]/div[2]/div/form/table/tbody/tr[3]/td[5]/div/span"))
                        .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\Westermo - GW2124p - 25.03.43.000\ORK-25.03.43.000.image"); // ok
                                                                                                                                                                   //
                        Thread.Sleep(30000);

                        driver.FindElement(By.XPath("/html/body/div/div[1]/button[2]"))
                           .Click();



                        Thread.Sleep(30000);
                    }
                    else
                    {
                        driver.FindElement(By.XPath("/html/body/div/div[2]/div[2]/div/form/table/tbody/tr[2]/td[5]/div/span"))
                           .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\Westermo - GW2124p - 25.03.43.000\ORK-25.03.43.000.image"); // ok

                        Thread.Sleep(30000);

                        driver.FindElement(By.XPath("/html/body/div/div[1]/button[2]"))
                           .Click();



                        Thread.Sleep(30000);
                    }


                    Thread.Sleep(30000);


                    await _log.LogAsync($"Update Realizado com sucesso");
                }

            }
            catch (Exception ex)
            {
                await _log.LogAsync($"Erro: {ex.Message}");
            }
            finally
            {
                driver.Quit();
            }

        }

        private async Task Router_ZIV(CancellationToken ct)
        {
            TimeSpan commandTimeout = TimeSpan.FromMinutes(3);

            // Inicia o serviço do ChromeDriver
            var service = ChromeDriverService.CreateDefaultService();
            service.Start(); // Garante que o serviço está ativo
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

            IWebDriver driver = new OpenQA.Selenium.Remote.RemoteWebDriver(
                service.ServiceUrl,
                options.ToCapabilities(),
                commandTimeout
            );

            try
            {
                ct.ThrowIfCancellationRequested();

                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                await Delay(1000, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[1]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerUser);
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[2]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerPass);
                await Delay(300, ct);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/div/input")).Click();
                await Delay(1000, ct);

                driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[1]/table/tbody/tr[3]/td/table/tbody/tr/td/table/tbody/tr[5]/td/a"))
                      .Click();
                await Delay(800, ct);

                var iframe = driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/div[1]/iframe"));
                driver.SwitchTo().Frame(iframe);

                driver.FindElement(By.XPath("/html/body/form/div[2]/table/tbody/tr[1]/td[2]/input"))
                      .SendKeys(Configuration.configurationValues.Path_ConfigFW + @"Firmware\Routers\ZIV - 4WF71090006-R008\emr4pro_3_41_8a_899c1e4a");

                await Delay(1000, ct);

                driver.FindElement(By.XPath("/html/body/form/div[2]/table/tbody/tr[3]/td[1]/input")).Click();

                // espera “longa” mas cancelável
                await Delay(3 * 60 * 1000, ct);

                await _log.LogAsync("Upgrade ZIV enviado.");
            }
            finally
            {
                try { driver.Quit(); } catch { }
            }
        }


        public static string GetExpectedFinalFw(string fabricante)
        {
            if (string.IsNullOrWhiteSpace(fabricante)) return "";

            var f = fabricante.ToUpperInvariant();

            if (f.Contains("ANDRA")) return "1.9.24.6";
            if (f.Contains("TELDAT")) return "11.01.10.45.03";
            if (f.Contains("VA") || f.Contains("WESTERMO")) return "25.03.43.000";
            if (f.Contains("ZIV")) return "3.41.8a"; // vindo de emr4pro_3_41_8a...

            return "";
        }

        public static bool FwMatches(string actual, string expected, string fabricante)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
                return false;

            // normaliza: remove espaços, hífen, underscore, etc.
            static string N(string s) =>
                System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "");

            var a = N(actual);
            var e = N(expected);

            // regra geral: "contém" chega (porque o router às vezes devolve prefixos/sufixos)
            if (a.Contains(e)) return true;

            // ZIV costuma aparecer como "3.41.8A" ou "3_41_8a" etc -> já cobre com a normalização,
            // mas fica aqui se um dia quiseres variantes.
            return false;
        }
    }

}
