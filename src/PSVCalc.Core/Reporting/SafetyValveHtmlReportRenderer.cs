using System.Text;

namespace PSVCalc.Core.Reporting;

public sealed class SafetyValveHtmlReportRenderer
{
    public string Render(SafetyValveReportDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
        sb.AppendLine("<head><meta charset=\"utf-8\">");
        AppendStyle(sb);
        sb.AppendLine("</head><body><div class=\"page\">");

        AppendHero(sb, document);
        AppendSummaryCards(sb, document.SummaryCards);
        AppendSection(sb, document.InputsTitle, document.InputRows);
        AppendSection(sb, document.ResultsTitle, document.ResultRows);
        AppendWarningsSection(sb, document.WarningsTitle, document.Warnings, document.NoneText);
        AppendSection(sb, document.ExpertTitle, document.ExpertRows);
        AppendSection(sb, document.AuditTitle, document.AuditRows);

        sb.AppendLine("<div class=\"footer\">Copyright \u00A9 2026 VictorMa</div>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendStyle(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("body{margin:0;background:#eef3f5;font-family:'Microsoft YaHei UI','Segoe UI',sans-serif;color:#233746;}");
        sb.AppendLine(".page{max-width:1120px;margin:0 auto;padding:24px;}");
        sb.AppendLine(".hero{width:100%;border-collapse:separate;border-spacing:0;background:linear-gradient(90deg,#5e8393,#496978);color:#fff;border-radius:16px;overflow:hidden;}");
        sb.AppendLine(".hero td{padding:22px;vertical-align:top;}");
        sb.AppendLine(".brand{font-size:13px;font-weight:600;opacity:.86;margin-bottom:8px;}");
        sb.AppendLine(".title{font-size:18px;font-weight:700;margin-bottom:8px;}");
        sb.AppendLine(".subtitle{font-size:13px;opacity:.92;}");
        sb.AppendLine(".hero-meta{width:280px;background:rgba(255,255,255,.08);}");
        sb.AppendLine(".meta-label{font-size:13px;opacity:.78;margin-bottom:4px;}");
        sb.AppendLine(".meta-value{font-size:13px;font-weight:600;margin-bottom:12px;}");
        sb.AppendLine(".cards{width:100%;border-collapse:separate;border-spacing:12px 0;margin-top:14px;}");
        sb.AppendLine(".card{background:#fff;border:1px solid #d8e1e7;border-radius:14px;padding:18px;}");
        sb.AppendLine(".card-title{font-size:18px;font-weight:700;color:#456271;margin-bottom:8px;}");
        sb.AppendLine(".card-label{font-size:13px;color:#7c909c;font-weight:600;margin-bottom:4px;}");
        sb.AppendLine(".card-value{font-size:13px;color:#233746;font-weight:600;}");
        sb.AppendLine(".section{width:100%;border-collapse:separate;border-spacing:0;margin-top:14px;background:#fff;border:1px solid #d8e1e7;border-radius:14px;overflow:hidden;}");
        sb.AppendLine(".section th{background:#f4f7f8;color:#456271;font-size:18px;font-weight:700;text-align:left;padding:14px 16px;border-bottom:1px solid #d8e1e7;}");
        sb.AppendLine(".section td{padding:11px 16px;border-bottom:1px solid #e8eef1;font-size:13px;vertical-align:top;}");
        sb.AppendLine(".section tr:last-child td{border-bottom:none;}");
        sb.AppendLine(".label{background:#f7fafb;color:#667e8a;font-weight:700;width:33%;}");
        sb.AppendLine(".warning{background:#fff7f4;color:#7a3f2e;font-weight:600;}");
        sb.AppendLine(".footer{margin-top:16px;text-align:center;color:#728591;font-size:13px;font-weight:600;}");
        sb.AppendLine("</style>");
    }

    private static void AppendHero(StringBuilder sb, SafetyValveReportDocument document)
    {
        sb.AppendLine("<table class=\"hero\"><tr>");
        sb.AppendLine("<td>");
        sb.AppendLine($"<div class=\"brand\">{H(document.AppTitle)}</div>");
        sb.AppendLine($"<div class=\"title\">{H(document.ReportTitle)}</div>");
        sb.AppendLine($"<div class=\"subtitle\">{H(document.CaseName)}</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("<td class=\"hero-meta\">");
        sb.AppendLine($"<div class=\"meta-label\">{H(document.GeneratedAtLabel)}</div><div class=\"meta-value\">{H(document.GeneratedAtValue)}</div>");
        sb.AppendLine($"<div class=\"meta-label\">{H(document.SoftwareVersionLabel)}</div><div class=\"meta-value\">{H(document.SoftwareVersionValue)}</div>");
        sb.AppendLine($"<div class=\"meta-label\">{H(document.StandardVersionLabel)}</div><div class=\"meta-value\">{H(document.StandardVersionValue)}</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("</tr></table>");
    }

    private static void AppendSummaryCards(StringBuilder sb, IReadOnlyList<ReportCard> cards)
    {
        sb.AppendLine("<table class=\"cards\"><tr>");
        string width = cards.Count == 0 ? "100%" : $"{100.0 / cards.Count:0.##}%";
        foreach (ReportCard card in cards)
        {
            sb.AppendLine($"<td style=\"width:{width}\"><div class=\"card\">");
            sb.AppendLine($"<div class=\"card-title\">{H(card.Title)}</div>");
            sb.AppendLine($"<div class=\"card-value\">{H(card.PrimaryValue)}</div>");
            foreach (ReportRow detail in card.Details)
            {
                sb.AppendLine($"<div class=\"card-label\" style=\"margin-top:10px\">{H(detail.Label)}</div>");
                sb.AppendLine($"<div class=\"card-value\">{H(detail.Value)}</div>");
            }

            sb.AppendLine("</div></td>");
        }

        sb.AppendLine("</tr></table>");
    }

    private static void AppendSection(StringBuilder sb, string title, IEnumerable<ReportRow> rows)
    {
        sb.AppendLine("<table class=\"section\">");
        sb.AppendLine($"<tr><th colspan=\"2\">{H(title)}</th></tr>");
        foreach (ReportRow row in rows)
        {
            sb.AppendLine($"<tr><td class=\"label\">{H(row.Label)}</td><td>{H(row.Value)}</td></tr>");
        }

        sb.AppendLine("</table>");
    }

    private static void AppendWarningsSection(StringBuilder sb, string title, IReadOnlyList<string> warnings, string noneText)
    {
        sb.AppendLine("<table class=\"section\">");
        sb.AppendLine($"<tr><th>{H(title)}</th></tr>");
        if (warnings.Count == 0)
        {
            sb.AppendLine($"<tr><td>{H(noneText)}</td></tr>");
        }
        else
        {
            foreach (string warning in warnings)
            {
                sb.AppendLine($"<tr><td class=\"warning\">{H(warning)}</td></tr>");
            }
        }

        sb.AppendLine("</table>");
    }

    private static string H(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }
}

