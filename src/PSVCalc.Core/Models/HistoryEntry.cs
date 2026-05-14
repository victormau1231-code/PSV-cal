using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Models;

public sealed class HistoryEntry
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string ProjectId { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public FluidType FluidType { get; set; }
    public double RequiredAreaMm2 { get; set; }
    public string SelectedOrifice { get; set; } = string.Empty;
    public string ProjectFile { get; set; } = string.Empty;
}

