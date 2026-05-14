namespace PSVCalc.Core.Reporting;

public sealed record ReportCard(string Title, string PrimaryValue, IReadOnlyList<ReportRow> Details);
