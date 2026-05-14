namespace PSVCalc.Core.Reporting;

public sealed class SafetyValveReportDocument
{
    public required string AppTitle { get; init; }
    public required string ReportTitle { get; init; }
    public required string CaseName { get; init; }
    public required string GeneratedAtLabel { get; init; }
    public required string GeneratedAtValue { get; init; }
    public required string SoftwareVersionLabel { get; init; }
    public required string SoftwareVersionValue { get; init; }
    public required string StandardVersionLabel { get; init; }
    public required string StandardVersionValue { get; init; }
    public required IReadOnlyList<ReportCard> SummaryCards { get; init; }
    public required string InputsTitle { get; init; }
    public required string ResultsTitle { get; init; }
    public required string WarningsTitle { get; init; }
    public required string ExpertTitle { get; init; }
    public required string AuditTitle { get; init; }
    public required string NoneText { get; init; }
    public required IReadOnlyList<ReportRow> InputRows { get; init; }
    public required IReadOnlyList<ReportRow> ResultRows { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public required IReadOnlyList<ReportRow> ExpertRows { get; init; }
    public required IReadOnlyList<ReportRow> AuditRows { get; init; }
}
