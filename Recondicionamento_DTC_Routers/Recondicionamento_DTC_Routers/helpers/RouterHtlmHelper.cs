using Recondicionamento_DTC_Routers.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Recondicionamento_DTC_Routers.helpers
{
    public static class RouterHtmlHelper
    {
        public static void EscreverHtmlInterativo(IReadOnlyList<RouterReportEntry> entries, string caminhoFicheiro)
        {
            entries ??= Array.Empty<RouterReportEntry>();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='UTF-8'><title>Lista de Routers</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th, td { border: 1px solid #ccc; padding: 8px; text-align: center; }");
            sb.AppendLine("th { background-color: #0078D7; color: white; position: sticky; top: 0; z-index: 1; }");
            sb.AppendLine("tbody tr:nth-child(even) { background: #f7f9fc; }");
            sb.AppendLine("tr.ok  { background: #e8f5e9 !important; }");
            sb.AppendLine("tr.nok { background: #ffebee !important; }");
            sb.AppendLine(".badge { display:inline-block; padding:2px 8px; border-radius:999px; font-size:12px; font-weight:700; }");
            sb.AppendLine(".badge-ok{ background:#2e7d32; color:#fff; }");
            sb.AppendLine(".badge-nok{ background:#c62828; color:#fff; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h2>Lista de Routers</h2>");

            sb.AppendLine("<table id='routerTable'><thead><tr>");
            sb.AppendLine("<th>Índice</th><th>Data</th><th>Fabricante</th><th>Número Série</th><th>Inspeção Visual</th><th>Liga</th><th>FW Old</th><th>FW New</th>");
            sb.AppendLine("<th>Config Upload</th><th>RS232</th><th>RS485</th>");
            sb.AppendLine("<th>Eth1</th><th>Eth2</th><th>Eth3</th><th>Eth4</th><th>Eth5</th><th>Eth6</th><th>Eth7</th><th>Eth8</th>");
            sb.AppendLine("<th>Antena</th><th>Cabo Alimentação</th><th>Cabo RS232</th><th>Cabo RS485</th><th>Comentários</th><th>Conformidade</th>");
            sb.AppendLine("</tr></thead><tbody>");

            int idx = 1;
            foreach (var e in entries)
            {
                var r = e.Record;
                bool conforme = r.ConformidadeFinal;

                sb.AppendLine(conforme ? "<tr class='ok'>" : "<tr class='nok'>");

                sb.AppendLine($"<td>{idx}</td>");
                sb.AppendLine($"<td>{E(e.Timestamp == default ? "" : e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))}</td>");
                sb.AppendLine($"<td>{E(r.Fabricante)}</td>");
                sb.AppendLine($"<td>{E(r.NumeroSerie)}</td>");
                sb.AppendLine($"<td>{E(r.InspecaoVisual)}</td>");
                sb.AppendLine($"<td>{E(r.Liga230V)}</td>");
                sb.AppendLine($"<td>{E(r.FirmwareOld)}</td>");
                sb.AppendLine($"<td>{E(r.FirmwareNew)}</td>");
                sb.AppendLine($"<td>{E(r.ConfigUploaded)}</td>");
                sb.AppendLine($"<td>{E(r.Rs232Score)}</td>");
                sb.AppendLine($"<td>{E(r.Rs485Ok)}</td>");

                for (int i = 0; i < 8; i++)
                {
                    if (r.EthOk == null || i >= r.EthOk.Length)
                        sb.AppendLine("<td>N.A.</td>");
                    else
                        sb.AppendLine($"<td>{E(r.EthOk[i])}</td>");
                }

                sb.AppendLine($"<td>{E(r.Antena)}</td>");
                sb.AppendLine($"<td>{E(r.CaboAlimentacao)}</td>");
                sb.AppendLine($"<td>{E(r.CaboRS232)}</td>");
                sb.AppendLine($"<td>{E(r.CaboRS485)}</td>");
                sb.AppendLine($"<td>{E(r.Comentario)}</td>");

                sb.AppendLine(conforme
                    ? "<td><span class='badge badge-ok'>Conforme</span></td>"
                    : "<td><span class='badge badge-nok'>Não conforme</span></td>");

                sb.AppendLine("</tr>");
                idx++;
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(caminhoFicheiro, sb.ToString(), Encoding.UTF8);
        }



        public static List<RouterReportEntry> LerHtmlParaLista(string caminhoFicheiro)
        {
            var list = new List<RouterReportEntry>();
            if (!File.Exists(caminhoFicheiro)) return list;

            string html = File.ReadAllText(caminhoFicheiro);

            var tbodyMatch = Regex.Match(html, @"<tbody[^>]*>(.*?)</tbody>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!tbodyMatch.Success) return list;

            string tbody = tbodyMatch.Groups[1].Value;

            var rows = Regex.Matches(tbody, @"<tr\b([^>]*)>(.*?)</tr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match row in rows)
            {
                string trAttrs = row.Groups[1].Value;
                string trInner = row.Groups[2].Value;

                var cells = Regex.Matches(trInner, @"<td\b[^>]*>(.*?)</td>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => CleanCell(m.Groups[1].Value))
                    .ToArray();

                // formato atual (com Data) = 25 colunas
                if (cells.Length < 24) continue;

                // Se cells[1] parece data => temos coluna Data
                bool hasDate = DateTime.TryParse(cells.ElementAtOrDefault(1), out var ts);

                // base indexes
                int iDate = hasDate ? 1 : -1;
                int iFab = hasDate ? 2 : 1;
                int iNs = hasDate ? 3 : 2;
                int iVis = hasDate ? 4 : 3;
                int iLig = hasDate ? 5 : 4;
                int iOld = hasDate ? 6 : 5;
                int iNew = hasDate ? 7 : 6;
                int iCfg = hasDate ? 8 : 7;
                int i232 = hasDate ? 9 : 8;
                int i485 = hasDate ? 10 : 9;
                int iEth = hasDate ? 11 : 10;
                int iAnt = hasDate ? 19 : 18;
                int iCal = hasDate ? 20 : 19;
                int iC23 = hasDate ? 21 : 20;
                int iC48 = hasDate ? 22 : 21;
                int iCom = hasDate ? 23 : 22;

                var entry = new RouterReportEntry();
                entry.Timestamp = hasDate ? ts : DateTime.MinValue;

                var r = new RouterRecord();
                r.Fabricante = cells.ElementAtOrDefault(iFab) ?? "";
                r.NumeroSerie = cells.ElementAtOrDefault(iNs) ?? "";
                r.InspecaoVisual = ParseBool(cells.ElementAtOrDefault(iVis) ?? "");
                r.Liga230V = ParseBool(cells.ElementAtOrDefault(iLig) ?? "");
                r.FirmwareOld = cells.ElementAtOrDefault(iOld) ?? "";
                r.FirmwareNew = cells.ElementAtOrDefault(iNew) ?? "";
                r.ConfigUploaded = ParseBool(cells.ElementAtOrDefault(iCfg) ?? "");

                int.TryParse(cells.ElementAtOrDefault(i232), out var rs232);
                r.Rs232Score = rs232;

                r.Rs485Ok = ParseBool(cells.ElementAtOrDefault(i485) ?? "");

                // Ethernet: lê até 8 colunas; pára se encontrar N.A.
                var eth = new List<bool>();
                for (int k = 0; k < 8; k++)
                {
                    var v = cells.ElementAtOrDefault(iEth + k) ?? "";
                    if (string.Equals(v, "N.A.", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(v, "NA", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(v, "N/A", StringComparison.OrdinalIgnoreCase))
                        break;

                    eth.Add(ParseBool(v));
                }
                r.EthOk = eth.ToArray();

                r.Antena = ParseBool(cells.ElementAtOrDefault(iAnt) ?? "");
                r.CaboAlimentacao = ParseBool(cells.ElementAtOrDefault(iCal) ?? "");
                r.CaboRS232 = ParseBool(cells.ElementAtOrDefault(iC23) ?? "");
                r.CaboRS485 = ParseBool(cells.ElementAtOrDefault(iC48) ?? "");
                r.Comentario = cells.ElementAtOrDefault(iCom) ?? "";

                // conformidade: usa class ok/nok (robusto)
                bool? conforme = null;
                var classMatch = Regex.Match(trAttrs, @"class\s*=\s*['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
                if (classMatch.Success)
                {
                    var cls = classMatch.Groups[1].Value;
                    if (Regex.IsMatch(cls, @"\bok\b", RegexOptions.IgnoreCase)) conforme = true;
                    if (Regex.IsMatch(cls, @"\bnok\b", RegexOptions.IgnoreCase)) conforme = false;
                }
                r.ConformidadeFinal = conforme ?? false;

                entry.Record = r;
                list.Add(entry);
            }

            return list;
        }



        private static string E(object value) => WebUtility.HtmlEncode(value?.ToString() ?? "");

        private static string CleanCell(string innerHtml)
        {
            string noTags = Regex.Replace(innerHtml, "<.*?>", "", RegexOptions.Singleline);
            return WebUtility.HtmlDecode(noTags).Trim();
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var v = s.Trim();

            if (v.Equals("True", StringComparison.OrdinalIgnoreCase)) return true;
            if (v.Equals("False", StringComparison.OrdinalIgnoreCase)) return false;

            // tolerâncias
            return v.Equals("Sim", StringComparison.OrdinalIgnoreCase)
                || v.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                || v.Equals("OK", StringComparison.OrdinalIgnoreCase)
                || v.Equals("1", StringComparison.OrdinalIgnoreCase);
        }
    }
}

public sealed class RouterReportEntry
{
    public DateTime Timestamp { get; set; }
    public RouterRecord Record { get; set; } = new RouterRecord();
}
