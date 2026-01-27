using Recondicionamento_DTC_Routers.Domain;
using System;
using System.IO;
using System.Text;

namespace Recondicionamento_DTC_Routers.helpers
{
    public static class DtcHtmlReport
    {
        public static void CreateEmpty(string reportPath)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
            html.AppendLine("<title>Report DTC</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f6f7fb}
                .wrap{padding:14px}
                h2{margin:0 0 12px 0}
                .card{background:#fff;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,.06);padding:12px}
                .table-wrap{overflow:auto;border-radius:10px}
                table{border-collapse:collapse;width:100%;min-width:1050px}
                thead th{position:sticky;top:0;background:#0b5cad;color:#fff;font-weight:600}
                th,td{border:1px solid #e4e6ef;padding:8px 10px;white-space:nowrap}
                tbody tr:nth-child(even){background:#fafbff}
                .badge{display:inline-block;padding:2px 8px;border-radius:999px;font-size:12px;font-weight:700}
                .ok{background:#d7f5dd;color:#0f6b2b}
                .fail{background:#ffd9d9;color:#8b0000}
                .skip{background:#e9eaef;color:#4a4a4a}
                .conf{background:#d7f5dd;color:#0f6b2b}
                .nconf{background:#ffd9d9;color:#8b0000}
                .muted{color:#666}
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<div class=\"wrap\">");
            html.AppendLine("<h2>Report DTC</h2>");
            html.AppendLine("<div class=\"card table-wrap\">");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th>#</th>");
            html.AppendLine("<th>Data</th>");
            html.AppendLine("<th>Fabricante</th>");
            html.AppendLine("<th>ID / Serial</th>");
            html.AppendLine("<th>FW Old</th>");
            html.AppendLine("<th>FW New</th>");
            html.AppendLine("<th>Config</th>");
            html.AppendLine("<th>s21 DTC</th>");
            html.AppendLine("<th>s21 EMI</th>");
            html.AppendLine("<th>Conformidade</th>");
            html.AppendLine("<th>Comentário</th>");
            html.AppendLine("</tr></thead>");
            html.AppendLine("<tbody>");
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</div></div>");
            html.AppendLine("</body></html>");

            File.WriteAllText(reportPath, html.ToString(), Encoding.UTF8);
        }

        public static void AppendRecord(string reportPath, DtcRecord r)
        {
            if (r == null) return;
            if (!File.Exists(reportPath))
                CreateEmpty(reportPath);

            // contar índice (simples): nº de <tr> existentes no tbody
            string content = File.ReadAllText(reportPath, Encoding.UTF8);
            int count = CountOccurrences(content, "<tr>");

            string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string fwOld = r.FirmwareOld ?? "";
            string fwNew = string.IsNullOrWhiteSpace(r.FirmwareNew) ? fwOld : (r.FirmwareNew ?? "");

            string row = $@"
<tr>
  <td class=""muted"">{count}</td>
  <td class=""muted"">{dt}</td>
  <td>{Html(r.Fabricante)}</td>
  <td>{Html(r.NumeroSerie)}</td>
  <td>{Html(fwOld)}</td>
  <td>{Html(fwNew)}</td>
  <td>{Badge(r.ConfigUploaded)}</td>
  <td>{Badge(r.AnalogOk)}</td>
  <td>{Badge(r.EmiPlcOk)}</td>
  <td>{BadgeConf(r.ConformidadeFinal)}</td>
  <td style=""white-space:normal;min-width:280px"">{Html(r.Comentario ?? "")}</td>
</tr>
";

            int idx = content.LastIndexOf("</tbody>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                File.AppendAllText(reportPath, row, Encoding.UTF8);
                return;
            }

            string updated = content.Insert(idx, row);
            File.WriteAllText(reportPath, updated, Encoding.UTF8);
        }

        private static string Badge(bool ok) =>
            ok ? "<span class=\"badge ok\">OK</span>" : "<span class=\"badge fail\">FAIL</span>";

        private static string BadgeConf(bool conf) =>
            conf ? "<span class=\"badge conf\">Conforme</span>" : "<span class=\"badge nconf\">Não conforme</span>";

        private static string Html(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static int CountOccurrences(string s, string token)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(token)) return 0;
            int c = 0, i = 0;
            while ((i = s.IndexOf(token, i, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                c++;
                i += token.Length;
            }
            // tira o header <tr> do thead: a primeira ocorrência é do cabeçalho
            // mas só se o HTML tiver thead. Aqui tem.
            if (c > 0) c -= 1;
            return c;
        }
    }
}
