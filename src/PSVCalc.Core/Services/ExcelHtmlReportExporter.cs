using System.Text;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Reporting;

namespace PSVCalc.Core.Services;

public sealed class ExcelHtmlReportExporter : IExcelReportExporter
{
    private readonly StoragePaths _paths;
    private readonly SafetyValveReportDataBuilder _dataBuilder = new();
    private readonly SafetyValveHtmlReportRenderer _renderer = new();

    public ExcelHtmlReportExporter(StoragePaths paths)
    {
        _paths = paths;
        _paths.EnsureDirectories();
    }

    public string Export(ProjectRecord record, UiLanguage language, string? preferredFileName = null)
    {
        if (record.Result is null)
        {
            throw new InvalidOperationException("Calculation result is required before exporting.");
        }

        string baseName = BuildSafeFileName(preferredFileName ?? $"{record.CaseName}-{DateTime.Now:yyyyMMdd-HHmmss}");
        string outputPath = Path.Combine(_paths.ExportsDirectory, $"{baseName}.xls");
        SafetyValveReportDocument document = _dataBuilder.Build(record, language);
        string html = _renderer.Render(document);

        File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return outputPath;
    }

    private static string BuildSafeFileName(string raw)
    {
        string safe = raw;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(safe) ? $"report-{DateTime.Now:yyyyMMdd-HHmmss}" : safe.Trim();
    }
}
