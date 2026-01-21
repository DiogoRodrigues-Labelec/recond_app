using FluentFTP;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Recondicionamento_DTC_Routers.Services;
using System;
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
            // ===== Paths locais (mantém a tua estrutura) =====
            string baseDir = Path.Combine(
                Configuration.configurationValues.Path_ConfigFW,
                @"Firmware\Routers\Teldat - 11.01.10.45.03\rs3g_1101104503_standard"
            );

            string localBios = Path.Combine(baseDir, "bmips34k.bin");
            string localFw = Path.Combine(baseDir, "fw00001d.bfw");
            string localCit = Path.Combine(baseDir, "rs3g.bin");

            if (!File.Exists(localBios)) throw new FileNotFoundException("BIOS não encontrado", localBios);
            if (!File.Exists(localFw)) throw new FileNotFoundException("FW não encontrado", localFw);
            if (!File.Exists(localCit)) throw new FileNotFoundException("CIT não encontrado", localCit);

            // ===== Credenciais =====
            string ftpIp = Configuration.configurationValues.ip;
            int ftpPort = 21; // normalmente 21; muda se o teu router usar outro
            string ftpUser = Configuration.configurationValues.routerUser;
            string ftpPass = Configuration.configurationValues.routerPass;

            // ===== Config FTP (timeouts “largos” p/ aguentar SAVEBUFFER) =====
            var cfg = new FtpConfig
            {
                ConnectTimeout = 20000,
                ReadTimeout = 140000,                 // >= 100s recomendado p/ comandos demorados
                DataConnectionConnectTimeout = 20000,
                DataConnectionReadTimeout = 140000,

                // Preferir ACTIVE/PORT (manual menciona issues quando não envolve porta 20)
                DataConnectionType = FtpDataConnectionType.PORT,

                // Evitar surpresas
                SocketKeepAlive = true,
                NoopInterval = 10,
            };

            using var client = new FtpClient(ftpIp, ftpUser, ftpPass, ftpPort, cfg);

            await _log.LogAsync($"[TELDAT] FTP connect {ftpIp}:{ftpPort} as '{ftpUser}'");
            ct.ThrowIfCancellationRequested();

            try
            {
                client.Connect();
                await _log.LogAsync("[TELDAT] Connected");

                // Binary mode (equivalente ao "bin"/TYPE I)
                client.Execute("TYPE I");
                await _log.LogAsync("[TELDAT] TYPE I (binary) OK");

                // Para não renomear automaticamente (se estiver ON no router)
                client.Execute("SITE CHECK RENAME OFF");
                await _log.LogAsync("[TELDAT] SITE CHECK RENAME OFF");

                // ===== Descobrir nome do CIT atual (recomendação do manual p/ não ficar com 2 CITs) =====
                string remoteCitName = "/" + Path.GetFileName(localCit); // fallback
                try
                {
                    var rep = client.Execute("SITE GETAPPNAME");
                    string msg = (rep.InfoMessages ?? "") + " " + (rep.Message ?? "");
                    // tenta apanhar algo tipo "appcode1.bin" / "*.bin"
                    var m = Regex.Match(msg, @"([A-Za-z0-9_\-]+\.bin)\b", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        remoteCitName = "/" + m.Groups[1].Value;
                        await _log.LogAsync($"[TELDAT] SITE GETAPPNAME -> '{m.Groups[1].Value}' (vou fazer upload do CIT para este nome)");
                    }
                    else
                    {
                        await _log.LogAsync($"[TELDAT] SITE GETAPPNAME sem nome *.bin (vou usar '{remoteCitName}')");
                    }
                }
                catch (Exception ex)
                {
                    await _log.LogAsync($"[TELDAT] WARN: falhou SITE GETAPPNAME ({ex.Message}). Vou usar '{remoteCitName}'.");
                }

                // ====== 1) Upload BIOS ======
                ct.ThrowIfCancellationRequested();
                await _log.LogAsync($"[TELDAT] Sending BIOS: {Path.GetFileName(localBios)} -> /{Path.GetFileName(localBios)}");
                client.UploadFile(localBios, "/" + Path.GetFileName(localBios), FtpRemoteExists.Overwrite, false, FtpVerify.None);
                await _log.LogAsync("[TELDAT] Sent BIOS (bmips34k.bin)");

                // ====== 2) SAVEBUFFER BIOS (pode demorar / “parecer” disconnect) ======
                ct.ThrowIfCancellationRequested();
                try
                {
                    var rep = client.Execute("SITE SAVEBUFFER");
                    await _log.LogAsync($"[TELDAT] BIOS: OK SITE SAVEBUFFER ({rep.Code}), waiting 4000ms");
                }
                catch (Exception ex)
                {
                    // Manual: alguns clientes cortam / acham que caiu durante SAVEBUFFER — tenta reconectar e seguir
                    await _log.LogAsync($"[TELDAT] BIOS: WARN SAVEBUFFER exception -> {ex.Message}");
                    try
                    {
                        if (client.IsConnected) client.Disconnect();
                    }
                    catch { }

                    await _log.LogAsync("[TELDAT] BIOS: Reconnecting after SAVEBUFFER warning...");
                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] BIOS: Reconnected + prepared");
                }

                await Task.Delay(4000, ct);

                // ====== 3) Upload Firmware (.bfw) ======
                // Tentativa #1: DIRECT OFF (normal)
                // Se falhar no PUT (ligação fechada / etc), tenta #2 DIRECT ON
                // Se falhar, tenta #3 mudando para PASV (às vezes redes fazem o inverso do esperado)
                string remoteFw = "/" + Path.GetFileName(localFw);

                bool fwOk = false;
                Exception lastFwEx = null;

                // attempt 1
                ct.ThrowIfCancellationRequested();
                await _log.LogAsync($"[TELDAT] Sending FW (attempt #1, PORT/ACTIVE, direct OFF): {Path.GetFileName(localFw)} -> {remoteFw}");
                try
                {
                    client.Execute("SITE DIRECT OFF");
                    client.UploadFile(localFw, remoteFw, FtpRemoteExists.Overwrite, false, FtpVerify.None);
                    fwOk = true;
                    await _log.LogAsync("[TELDAT] FW attempt #1 OK");
                }
                catch (Exception ex1)
                {
                    lastFwEx = ex1;
                    await _log.LogAsync("[TELDAT] FW attempt #1 FAILED");
                    await _log.LogAsync($"[TELDAT] EX1: {ex1}");
                }

                // attempt 2 (DIRECT ON)
                if (!fwOk)
                {
                    ct.ThrowIfCancellationRequested();

                    await _log.LogAsync("[TELDAT] Reconnecting before FW attempt #2...");
                    try { if (client.IsConnected) client.Disconnect(); } catch { }
                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] Reconnected + prepared");

                    await _log.LogAsync($"[TELDAT] Sending FW (attempt #2, PORT/ACTIVE, direct ON): {Path.GetFileName(localFw)} -> {remoteFw}");
                    try
                    {
                        client.Execute("SITE DIRECT ON");
                        await _log.LogAsync("[TELDAT] FW#2: OK SITE DIRECT ON");

                        client.UploadFile(localFw, remoteFw, FtpRemoteExists.Overwrite, false, FtpVerify.None);
                        fwOk = true;
                        await _log.LogAsync("[TELDAT] FW attempt #2 OK");
                    }
                    catch (Exception ex2)
                    {
                        lastFwEx = ex2;
                        await _log.LogAsync("[TELDAT] FW attempt #2 FAILED");
                        await _log.LogAsync($"[TELDAT] EX2: {ex2}");
                    }
                    finally
                    {
                        // desligar DIRECT se possível
                        try { client.Execute("SITE DIRECT OFF"); } catch { }
                    }
                }

                // attempt 3 (mudar para PASV)
                if (!fwOk)
                {
                    ct.ThrowIfCancellationRequested();

                    await _log.LogAsync("[TELDAT] Reconnecting before FW attempt #3 (PASV)...");
                    try { if (client.IsConnected) client.Disconnect(); } catch { }

                    // muda o modo
                    client.Config.DataConnectionType = FtpDataConnectionType.PASV;

                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] Reconnected + prepared (PASV)");

                    await _log.LogAsync($"[TELDAT] Sending FW (attempt #3, PASV, direct ON): {Path.GetFileName(localFw)} -> {remoteFw}");
                    try
                    {
                        client.Execute("SITE DIRECT ON");
                        await _log.LogAsync("[TELDAT] FW#3: OK SITE DIRECT ON");

                        client.UploadFile(localFw, remoteFw, FtpRemoteExists.Overwrite, false, FtpVerify.None);
                        fwOk = true;
                        await _log.LogAsync("[TELDAT] FW attempt #3 OK");
                    }
                    catch (Exception ex3)
                    {
                        lastFwEx = ex3;
                        await _log.LogAsync("[TELDAT] FW attempt #3 FAILED");
                        await _log.LogAsync($"[TELDAT] EX3: {ex3}");
                    }
                    finally
                    {
                        try { client.Execute("SITE DIRECT OFF"); } catch { }
                    }
                }

                if (!fwOk)
                {
                    throw new Exception(
                        "FW upload falhou (router fecha a ligação durante STOR). " +
                        $"Último erro: {lastFwEx?.Message}", lastFwEx
                    );
                }

                // ====== 4) SAVEBUFFER FW ======
                ct.ThrowIfCancellationRequested();
                try
                {
                    var rep = client.Execute("SITE SAVEBUFFER");
                    await _log.LogAsync($"[TELDAT] FW: OK SITE SAVEBUFFER ({rep.Code}), waiting 6000ms");
                }
                catch (Exception ex)
                {
                    await _log.LogAsync($"[TELDAT] FW: WARN SAVEBUFFER exception -> {ex.Message}");
                    try { if (client.IsConnected) client.Disconnect(); } catch { }

                    await _log.LogAsync("[TELDAT] FW: Reconnecting after SAVEBUFFER warning...");
                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] FW: Reconnected + prepared");
                }

                await Task.Delay(6000, ct);

                // ====== 5) Upload CIT (idealmente para o nome do CIT atual) ======
                ct.ThrowIfCancellationRequested();
                await _log.LogAsync($"[TELDAT] Sending CIT: {Path.GetFileName(localCit)} -> {remoteCitName}");

                // Geralmente CIT é grande → DIRECT ON recomendado como fallback (manual)
                bool citOk = false;
                Exception lastCitEx = null;

                // CIT attempt 1 (direct OFF)
                try
                {
                    client.Execute("SITE DIRECT OFF");
                    client.UploadFile(localCit, remoteCitName, FtpRemoteExists.Overwrite, false, FtpVerify.None);
                    citOk = true;
                    await _log.LogAsync("[TELDAT] CIT attempt #1 OK");
                }
                catch (Exception ex1)
                {
                    lastCitEx = ex1;
                    await _log.LogAsync("[TELDAT] CIT attempt #1 FAILED");
                    await _log.LogAsync($"[TELDAT] CIT EX1: {ex1}");
                }

                // CIT attempt 2 (direct ON)
                if (!citOk)
                {
                    ct.ThrowIfCancellationRequested();
                    await _log.LogAsync("[TELDAT] Reconnecting before CIT attempt #2 (DIRECT ON)...");
                    try { if (client.IsConnected) client.Disconnect(); } catch { }

                    // volta ao modo mais “provável” (ACTIVE) para o CIT
                    client.Config.DataConnectionType = FtpDataConnectionType.PORT;

                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] Reconnected + prepared (CIT)");

                    try
                    {
                        client.Execute("SITE DIRECT ON");
                        await _log.LogAsync("[TELDAT] CIT#2: OK SITE DIRECT ON");

                        client.UploadFile(localCit, remoteCitName, FtpRemoteExists.Overwrite, false, FtpVerify.None);
                        citOk = true;
                        await _log.LogAsync("[TELDAT] CIT attempt #2 OK");
                    }
                    catch (Exception ex2)
                    {
                        lastCitEx = ex2;
                        await _log.LogAsync("[TELDAT] CIT attempt #2 FAILED");
                        await _log.LogAsync($"[TELDAT] CIT EX2: {ex2}");
                    }
                    finally
                    {
                        try { client.Execute("SITE DIRECT OFF"); } catch { }
                    }
                }

                if (!citOk)
                {
                    throw new Exception($"CIT upload falhou. Último erro: {lastCitEx?.Message}", lastCitEx);
                }

                // ====== 6) SAVEBUFFER CIT ======
                ct.ThrowIfCancellationRequested();
                try
                {
                    var rep = client.Execute("SITE SAVEBUFFER");
                    await _log.LogAsync($"[TELDAT] CIT: OK SITE SAVEBUFFER ({rep.Code}), waiting 8000ms");
                }
                catch (Exception ex)
                {
                    await _log.LogAsync($"[TELDAT] CIT: WARN SAVEBUFFER exception -> {ex.Message}");
                    try { if (client.IsConnected) client.Disconnect(); } catch { }

                    await _log.LogAsync("[TELDAT] CIT: Reconnecting after SAVEBUFFER warning...");
                    client.Connect();
                    client.Execute("TYPE I");
                    client.Execute("SITE CHECK RENAME OFF");
                    await _log.LogAsync("[TELDAT] CIT: Reconnected + prepared");
                }

                await Task.Delay(8000, ct);

                // ====== 7) COHERENCE (opcional mas útil) ======
                ct.ThrowIfCancellationRequested();
                try
                {
                    var rep = client.Execute("SITE COHERENCE");
                    await _log.LogAsync($"[TELDAT] SITE COHERENCE -> {rep.Code} {rep.Message}".Trim());
                    if (!string.IsNullOrWhiteSpace(rep.InfoMessages))
                        await _log.LogAsync($"[TELDAT] COHERENCE INFO:\n{rep.InfoMessages}".Trim());
                }
                catch (Exception ex)
                {
                    await _log.LogAsync($"[TELDAT] WARN: SITE COHERENCE falhou: {ex.Message}");
                }

                // ====== 8) RELOAD ======
                ct.ThrowIfCancellationRequested();
                client.Execute("SITE RELOAD ON");
                await _log.LogAsync("[TELDAT] Comando restart enviado (SITE RELOAD ON). Tipicamente reinicia após ~30s ao sair do FTP.");

                try { client.Disconnect(); } catch { }
                await _log.LogAsync("[TELDAT] FTP Disconnect");
                await _log.LogAsync("[TELDAT] Transferencia de ficheiros realizada com sucesso");
            }
            catch (Exception ex)
            {
                await _log.LogAsync($"[TELDAT] FTP Error (FULL): {ex}");
                if (ex.InnerException != null)
                    await _log.LogAsync($"[TELDAT] FTP InnerException: {ex.InnerException}");

                throw;
            }
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
