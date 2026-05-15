using PSVCalc.Core.Models;
using PSVCalc.Core.Reporting;

namespace PSVCalc.Tests;

public sealed class ValidationHtmlReportRendererTests
{
    [Fact]
    public void Render_ShouldIncludeSummaryRowsAndEscapeCaseText()
    {
        var renderer = new ValidationHtmlReportRenderer();
        var summary = new ValidationRunSummary
        {
            RunAt = new DateTimeOffset(2026, 5, 13, 12, 0, 0, TimeSpan.Zero),
            SetName = "Field <Cases>",
            StandardVersion = "API 520/521 + ASME",
            Results =
            [
                new ValidationCaseResult
                {
                    CaseId = "Case-001",
                    Description = "Good gas case",
                    Passed = true,
                    ExpectedRequiredAreaMm2 = 100,
                    ActualRequiredAreaMm2 = 100.1,
                    AreaDeviationPercent = 0.1,
                    ExpectedOrificeLetter = "E",
                    ActualOrificeLetter = "E"
                },
                new ValidationCaseResult
                {
                    CaseId = "Case-002",
                    Description = "Needs review",
                    Passed = false,
                    ExpectedRequiredAreaMm2 = 200,
                    ActualRequiredAreaMm2 = 260,
                    AreaDeviationPercent = 30,
                    ExpectedOrificeLetter = "F",
                    ActualOrificeLetter = "G",
                    ErrorMessage = "Area > limit"
                }
            ]
        };

        string html = renderer.Render(summary, Localize);

        Assert.Contains("PSV Calculator Pro V 1.3.2", html);
        Assert.Contains("Validation Report", html);
        Assert.Contains("Field &lt;Cases&gt;", html);
        Assert.Contains("Case-001", html);
        Assert.Contains("Case-002", html);
        Assert.Contains("PASS", html);
        Assert.Contains("FAIL", html);
        Assert.Contains("<div class=\"card-label\">Status</div><div class=\"card-value\">FAIL</div>", html);
        Assert.Contains("50%", html);
        Assert.Contains("Copyright \u00A9 2026 VictorMa", html);
    }

    private static string Localize(string key)
    {
        return key switch
        {
            "app_title" => "PSV Calculator Pro V 1.3.2",
            "validation_report" => "Validation Report",
            "generated_at" => "Generated At",
            "software_version" => "Software Version",
            "standard_version" => "Standard Version",
            "validation_summary" => "Validation Summary",
            "validation_set" => "Validation Set",
            "validation_status" => "Status",
            "validation_status_passed" => "PASS",
            "validation_status_failed" => "FAIL",
            "validation_pass_rate" => "Pass Rate",
            "validation_case_id" => "Case ID",
            "validation_description" => "Description",
            "expected_area" => "Expected Area",
            "actual_area" => "Actual Area",
            "deviation_percent" => "Deviation %",
            "expected_orifice" => "Expected Orifice",
            "actual_orifice" => "Actual Orifice",
            "error_message" => "Error",
            _ => key
        };
    }
}


