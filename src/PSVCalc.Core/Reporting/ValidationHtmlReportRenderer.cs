using System.Globalization;
using System.Text;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Reporting;

public sealed class ValidationHtmlReportRenderer
{
    public string Render(ValidationRunSummary summary, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(localize);

        string passRate = summary.Total == 0
            ? "0%"
            : $"{summary.Passed * 100.0 / summary.Total:0.##}%";

        string L(string key) => localize(key) ?? key;

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>Validation Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{margin:0;background:#eef3f5;font-family:'Microsoft YaHei UI','Segoe UI',sans-serif;color:#233746;}");
        sb.AppendLine(".page{max-width:1120px;margin:0 auto;padding:24px 24px 32px;}");
        sb.AppendLine(".hero{width:100%;border-collapse:separate;border-spacing:0;background:linear-gradient(90deg,#5e8393,#496978);color:#fff;border-radius:16px;overflow:hidden;}");
        sb.AppendLine(".hero td{padding:22px;vertical-align:top;}");
        sb.AppendLine(".brand{font-size:13px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;opacity:.82;margin-bottom:8px;}");
        sb.AppendLine(".hero-title{font-size:18px;font-weight:700;margin-bottom:8px;}");
        sb.AppendLine(".hero-sub{font-size:13px;opacity:.92;}");
        sb.AppendLine(".hero-meta{width:280px;background:rgba(255,255,255,.08);}");
        sb.AppendLine(".meta-label{font-size:13px;opacity:.78;margin-bottom:4px;}");
        sb.AppendLine(".meta-value{font-size:13px;font-weight:600;margin-bottom:12px;}");
        sb.AppendLine(".cards{width:100%;border-collapse:separate;border-spacing:12px 0;margin-top:14px;}");
        sb.AppendLine(".card{background:#fff;border:1px solid #d8e1e7;border-radius:14px;padding:18px;}");
        sb.AppendLine(".card-title{font-size:18px;font-weight:700;color:#456271;margin-bottom:10px;}");
        sb.AppendLine(".card-label{font-size:13px;color:#7c909c;font-weight:600;margin-bottom:4px;}");
        sb.AppendLine(".card-value{font-size:13px;color:#233746;font-weight:600;}");
        sb.AppendLine(".section{width:100%;border-collapse:separate;border-spacing:0;margin-top:14px;background:#fff;border:1px solid #d8e1e7;border-radius:14px;overflow:hidden;}");
        sb.AppendLine(".section th{background:#f4f7f8;color:#456271;font-size:18px;font-weight:700;text-align:left;padding:14px 16px;border-bottom:1px solid #d8e1e7;}");
        sb.AppendLine(".section td{padding:11px 16px;border-bottom:1px solid #e8eef1;font-size:13px;vertical-align:top;}");
        sb.AppendLine(".section tr:last-child td{border-bottom:none;}");
        sb.AppendLine(".head{background:#f7fafb;color:#667e8a;font-weight:700;}");
        sb.AppendLine(".ok{color:#3f775b;font-weight:700;}");
        sb.AppendLine(".fail{color:#9e2a2b;font-weight:700;}");
        sb.AppendLine(".footer{margin-top:16px;text-align:center;color:#728591;font-size:13px;font-weight:600;}");
        sb.AppendLine("</style></head><body><div class=\"page\">");

        sb.AppendLine("<table class=\"hero\"><tr>");
        sb.AppendLine("<td>");
        sb.AppendLine($"<div class=\"brand\">{H(L("app_title"))}</div>");
        sb.AppendLine($"<div class=\"hero-title\">{H(L("validation_report"))}</div>");
        sb.AppendLine($"<div class=\"hero-sub\">{H(summary.SetName)}</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("<td class=\"hero-meta\">");
        sb.AppendLine($"<div class=\"meta-label\">{H(L("generated_at"))}</div><div class=\"meta-value\">{H(summary.RunAt.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
        sb.AppendLine($"<div class=\"meta-label\">{H(L("software_version"))}</div><div class=\"meta-value\">{H(AppMetadata.SoftwareVersion)}</div>");
        sb.AppendLine($"<div class=\"meta-label\">{H(L("standard_version"))}</div><div class=\"meta-value\">{H(summary.StandardVersion)}</div>");
        sb.AppendLine("</td>");
        sb.AppendLine("</tr></table>");

        sb.AppendLine("<table class=\"cards\"><tr>");
        sb.AppendLine("<td style=\"width:33.33%\"><div class=\"card\">");
        sb.AppendLine($"<div class=\"card-title\">{H(L("validation_summary"))}</div>");
        sb.AppendLine($"<div class=\"card-label\">{H(L("validation_set"))}</div><div class=\"card-value\">{H(summary.SetName)}</div>");
        sb.AppendLine("</div></td>");
        sb.AppendLine("<td style=\"width:33.33%\"><div class=\"card\">");
        sb.AppendLine($"<div class=\"card-title\">{summary.Passed}/{summary.Total}</div>");
        string summaryStatus = summary.Failed > 0
            ? L("validation_status_failed")
            : L("validation_status_passed");
        sb.AppendLine($"<div class=\"card-label\">{H(L("validation_status"))}</div><div class=\"card-value\">{H(summaryStatus)}</div>");
        sb.AppendLine("</div></td>");
        sb.AppendLine("<td style=\"width:33.33%\"><div class=\"card\">");
        sb.AppendLine($"<div class=\"card-title\">{H(passRate)}</div>");
        sb.AppendLine($"<div class=\"card-label\">{H(L("validation_pass_rate"))}</div><div class=\"card-value\">{summary.Failed} {H(L("validation_status_failed"))}</div>");
        sb.AppendLine("</div></td>");
        sb.AppendLine("</tr></table>");

        sb.AppendLine("<table class=\"section\">");
        sb.AppendLine($"<tr><th colspan=\"9\">{H(L("validation_report"))}</th></tr>");
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td class=\"head\">{H(L("validation_case_id"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("validation_description"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("validation_status"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("expected_area"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("actual_area"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("deviation_percent"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("expected_orifice"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("actual_orifice"))}</td>");
        sb.AppendLine($"<td class=\"head\">{H(L("error_message"))}</td>");
        sb.AppendLine("</tr>");

        foreach (ValidationCaseResult item in summary.Results)
        {
            string statusClass = item.Passed ? "ok" : "fail";
            string statusText = item.Passed ? L("validation_status_passed") : L("validation_status_failed");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{H(item.CaseId)}</td>");
            sb.AppendLine($"<td>{H(item.Description)}</td>");
            sb.AppendLine($"<td class=\"{statusClass}\">{H(statusText)}</td>");
            sb.AppendLine($"<td>{H(item.ExpectedRequiredAreaMm2.ToString("G10", CultureInfo.InvariantCulture))}</td>");
            sb.AppendLine($"<td>{H(item.ActualRequiredAreaMm2.ToString("G10", CultureInfo.InvariantCulture))}</td>");
            sb.AppendLine($"<td>{H(item.AreaDeviationPercent.ToString("G10", CultureInfo.InvariantCulture))}</td>");
            sb.AppendLine($"<td>{H(item.ExpectedOrificeLetter)}</td>");
            sb.AppendLine($"<td>{H(item.ActualOrificeLetter)}</td>");
            sb.AppendLine($"<td>{H(item.ErrorMessage ?? string.Empty)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("<div class=\"footer\">Copyright \u00A9 2026 VictorMa</div>");
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    public void WriteToFile(string outputPath, ValidationRunSummary summary, Func<string, string> localize)
    {
        string html = Render(summary, localize);
        File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
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


