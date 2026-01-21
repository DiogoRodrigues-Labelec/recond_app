using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.Services
{
    public sealed class HttpProbeService
    {
        private static HttpClient CreateClient(string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage req, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) => true
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<(HttpStatusCode? code, string body, string fabricante)> ProbeAsync(string ip, string port, CancellationToken ct)
        {
            // tenta HTTP e depois HTTPS
            var httpUrl = $"http://{ip}:{port}/";
            var httpsUrl = $"https://{ip}:{port}/";

            var r1 = await TryGetAsync(httpUrl, ct);
            if (r1.code.HasValue) return (r1.code, r1.body, DetectFabricante(r1.body));

            var r2 = await TryGetAsync(httpsUrl, ct);
            return (r2.code, r2.body, DetectFabricante(r2.body));
        }

        private static async Task<(HttpStatusCode? code, string body)> TryGetAsync(string baseUrl, CancellationToken ct)
        {
            try
            {
                using var client = CreateClient(baseUrl);
                using var resp = await client.GetAsync("", HttpCompletionOption.ResponseContentRead, ct);
                string body = await resp.Content.ReadAsStringAsync(ct);
                return (resp.StatusCode, body);
            }
            catch (OperationCanceledException) { return (null, ""); }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException) { return (null, ""); }
            catch { return (null, ""); }
        }

        public static string DetectFabricante(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "UNKNOWN";

            if (body.Contains("ZIV")) return "ZIV";
            if (body.Contains("HTTP-EQUIV=\"REFRESH\"")) return "ANDRA";
            if (body.Contains("Teldat")) return "TELDAT";
            if (body.Contains("LuCI - Lua Configuration Interface")) return "VA";
            if (body.Contains("CIRCUTOR") || body.Contains("Circutor")) return "CIRCUTOR";

            return "UNKNOWN";
        }
    }
}
