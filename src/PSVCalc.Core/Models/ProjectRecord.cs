using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Models;

public sealed class ProjectRecord
{
    public string ProjectId { get; set; } = Guid.NewGuid().ToString("N");
    public string CaseName { get; set; } = "New Case";
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.Now;
    public string SoftwareVersion { get; set; } = AppMetadata.SoftwareVersion;
    public string StandardVersion { get; set; } = AppMetadata.StandardVersion;
    public UiLanguage Language { get; set; } = UiLanguage.ZhCn;
    public required CalculationInput Input { get; set; }
    public CalculationResult? Result { get; set; }
}

